using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<MessageDto> RelayMessage(MessageDto message)
    {
        if (!SupportedProviders.Contains(message.Provider))
            throw new MnemaException($"Provider {message.Provider} is not supported");

        object? data = message.Type switch {
            MessageType.ListContent => await ListContent(message),
            MessageType.FilterContent => await FilterContent(message.ContentId, message.Data),
            MessageType.StartDownload => await StartDownload(message.ContentId),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.Type, "Unsupported message type")
        };

        return new MessageDto
        {
            Provider = message.Provider,
            ContentId = message.ContentId,
            Type = message.Type,
            Data = data == null ? null : JsonSerializer.Serialize(data, JsonSerializerOptions),
        };
    }

    private async Task<object?> FilterContent(string hash, JsonNode? node)
    {
        var ids = node?.Deserialize<List<string>>();
        if (ids == null) return null;

        var client = await GetQBittorrentClient();
        if (client == null) return null;

        var files = await client.GetTorrentContentsAsync(hash);
        if (files == null) return null;

        var toDownload = new HashSet<int>();
        var toSkip = new HashSet<int>();

        foreach (var file in files)
        {
            if (file.Index == null) continue;

            if (ids.Contains(file.Name))
            {
                if (file.Priority != TorrentContentPriority.Minimal)
                    toDownload.Add(file.Index.Value);
            }
            else
            {
                if (file.Priority != TorrentContentPriority.Skip)
                    toSkip.Add(file.Index.Value);
            }
        }

        if (toDownload.Count > 0)
            await client.SetFilePriorityAsync(hash, toDownload, TorrentContentPriority.Minimal);

        if (toSkip.Count > 0)
            await client.SetFilePriorityAsync(hash, toSkip, TorrentContentPriority.Skip);

        return null;
    }

    private async Task<object?> StartDownload(string hash)
    {
        var client = await GetQBittorrentClient();
        if (client == null) return null;

        await client.ResumeAsync([hash], CancellationToken.None);

        return null;
    }

    private async Task<List<ListContentData>?> ListContent(MessageDto message)
    {
        var client = await GetQBittorrentClient();
        if (client == null) return null;

        var hash = message.ContentId;

        var content = await client.GetTorrentContentsAsync(hash);
        if (content == null) return null;

        return BuildTree(content);
    }

    private List<ListContentData> BuildTree(IReadOnlyList<TorrentContent> files, int depth = 0)
    {
        var tree = new List<ListContentData>();

        var filesByFirstDir = files
            .GroupBy(file =>
            {
                var branch = file.Name.Split('/');
                return depth >= branch.Length ? string.Empty : branch[depth];
            });

        foreach (var group in filesByFirstDir)
        {
            var dir = group.Key;
            if (string.IsNullOrEmpty(dir))
                continue;

            var fileGroup = group.ToList();
            var firstFile = fileGroup[0];
            var branch = firstFile.Name.Split('/');

            // Leaf node (file)
            if (branch.Length == depth + 1)
            {
                var id = firstFile.Name;
                var totalBytes = firstFile.Size.AsHumanReadableSize();

                tree.Add(new ListContentData
                {
                    Label = $"{dir} {totalBytes}",
                    Selected = firstFile.Priority > TorrentContentPriority.Skip,
                    SubContentId = id
                });
                continue;
            }

            // Directory → recurse
            var children = BuildTree(fileGroup, depth + 1);
            children.Sort((a, b)
                => string.Compare(a.Label, b.Label, StringComparison.Ordinal));

            tree.Add(new ListContentData
            {
                Label = dir,
                Children = children
            });
        }

        // Collapse single root directory
        if (tree.Count == 1 && string.IsNullOrEmpty(tree[0].SubContentId))
        {
            tree = tree[0].Children ?? [];
        }

        return tree;
    }
}
