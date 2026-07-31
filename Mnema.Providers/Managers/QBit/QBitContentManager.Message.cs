using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;
using QBittorrent.Client;

namespace Mnema.Providers.Managers.QBit;

internal partial class QBitContentManager
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<MessageDto> RelayMessage(MessageDto message, CancellationToken ct = default)
    {
        if (!SupportedProviders.Contains(message.Provider))
            throw new MnemaException($"Provider {message.Provider} is not supported");

        var data = message.Type switch {
            MessageType.ListContent => await ListContent(message, ct),
            MessageType.FilterContent => await FilterContent(message, ct),
            MessageType.StartDownload => await StartDownload(message, ct),
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

    private async Task<object?> FilterContent(MessageDto message, CancellationToken ct)
    {
        var selectedIds = message.Data.Deserialize<List<string>>(JsonSerializerOptions);
        if (selectedIds == null) return null;

        var externalDownload = await GetExternalDownload(message.ContentId, ct);

        await FilterContent(externalDownload.ExternalId, currentlyEnabled =>
        {
            var allSeriesPaths = externalDownload.Files.Select(f => f.FullPath);

            return currentlyEnabled
                .Except(allSeriesPaths)
                .Concat(selectedIds)
                .ToList();
        }, ct);

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.GetRequiredService<IUnitOfWork>();

        externalDownload.Files.ForEach(file =>
        {
            file.Selected = selectedIds.Contains(file.FullPath);
        });

        unitOfWork.ExternalDownloadRepository.Update(externalDownload);
        await unitOfWork.CommitAsync(ct);

        return null;
    }

    private async Task FilterContent(string hash, Func<List<string>, List<string>> idsFunc, CancellationToken ct = default)
    {
        var files = await qBitClient.GetTorrentContentsAsync(hash, ct);
        if (files == null)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            files = await qBitClient.GetTorrentContentsAsync(hash, ct);
        }

        var currentEnabled = files
            .Where(f => f.Priority != TorrentContentPriority.Skip)
            .Select(f => f.Name)
            .ToList();
        var ids = idsFunc(currentEnabled);

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
            await qBitClient.SetFilePriorityAsync(hash, toDownload, TorrentContentPriority.Minimal, ct);

        if (toSkip.Count > 0)
            await qBitClient.SetFilePriorityAsync(hash, toSkip, TorrentContentPriority.Skip, ct);
    }

    private async Task<object?> StartDownload(MessageDto message, CancellationToken ct)
    {
        var externalDownload = await GetExternalDownload(message.ContentId, ct);

        await qBitClient.ResumeTorrentsAsync([externalDownload.ExternalId], CancellationToken.None);

        using var scope = scopeFactory.CreateScope();
        var messageService = scope.GetRequiredService<IMessageService>();

        await messageService.RefreshDashboard(externalDownload.UserId);

        return null;
    }

    private async Task<List<ListContentData>?> ListContent(MessageDto message, CancellationToken cancellationToken)
    {
        var externalDownload = await GetExternalDownload(message.ContentId, cancellationToken);

        return BuildTree(externalDownload.Files);
    }

    private List<ListContentData> BuildTree(IReadOnlyList<ExternalDownloadFile> files, int depth = 0)
    {
        var tree = new List<ListContentData>();

        var filesByFirstDir = files
            .GroupBy(file =>
            {
                var branch = file.FullPath.Split('/');
                return depth >= branch.Length ? string.Empty : branch[depth];
            });

        foreach (var group in filesByFirstDir)
        {
            var dir = group.Key;
            if (string.IsNullOrEmpty(dir))
                continue;

            var fileGroup = group.ToList();
            var firstFile = fileGroup[0];
            var branch = firstFile.FullPath.Split('/');

            // Leaf node (file)
            if (branch.Length == depth + 1)
            {
                var id = firstFile.FullPath;
                var totalBytes = firstFile.FileSize.AsHumanReadableSize();

                tree.Add(new ListContentData
                {
                    Label = $"{dir} {totalBytes}",
                    Selected = firstFile.Selected,
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

    private async Task<ExternalDownload> GetExternalDownload(string id, CancellationToken ct = default)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            throw new BadRequestException($"{id} is not a valid guid");
        }

        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.GetRequiredService<IUnitOfWork>();

        var externalDownload = await unitOfWork.ExternalDownloadRepository.GetById(guid, ct);
        if (externalDownload == null)
        {
            throw new BadRequestException($"{id} is not a valid external download");
        }

        return externalDownload;
    }
}
