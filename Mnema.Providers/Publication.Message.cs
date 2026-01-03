using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Publication;

namespace Mnema.Providers;

internal partial class Publication
{
    public Task<MessageDto> ProcessMessage(MessageDto message)
    {
        return message.Type switch
        {
            MessageType.ListContent => ListContent(),
            MessageType.FilterContent => FilterContent(message),
            MessageType.StartDownload => StartDownload(),
            _ => throw new MnemaException("Unknown message type: " + message.Type)
        };
    }

    private Task<MessageDto> ListContent()
    {
        if (Series == null || Series.Chapters.Count == 0)
        {
            return Task.FromResult(new MessageDto
            {
                Provider = provider,
                ContentId = Id,
                Type = MessageType.ListContent,
            });
        }
        
        var data = Series.Chapters.GroupBy(c => c.VolumeMarker)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sortSlice = data.Keys.ToList();
        sortSlice.Sort(SortFloatStrings);

        var content = sortSlice.SelectMany(volume =>
        {
            var chapters = data[volume];
            
            if (string.IsNullOrEmpty(volume) && sortSlice.Count == 1)
            {
                return CreateChildren(chapters);
            }

            return [new ListContentData
            {
                Label = string.IsNullOrEmpty(volume) ? "No Volume" : $"Volume {volume}",
                Children = CreateChildren(chapters),
            }];
        });

        return Task.FromResult(new MessageDto()
        {
            ContentId = Id,
            Provider = provider,
            Type = MessageType.ListContent,
            Data = JsonSerializer.Serialize(content, new JsonSerializerOptions { PropertyNamingPolicy =  JsonNamingPolicy.CamelCase }), 
        });
        
        bool WillBeDownloaded(Chapter chapter)
        {
            return _userSelectedIds.Count > 0 ? _userSelectedIds.Contains(chapter.Id) : _queuedChapters.Contains(chapter.Id);
        }
        
        List<ListContentData> CreateChildren(List<Chapter> volumeChapters)
        {
            // Sort chapters
            volumeChapters.Sort((a, b) =>
            {
                if (a.VolumeMarker != b.VolumeMarker)
                {
                    return (int)((b.VolumeNumber() ?? -1) - (a.VolumeNumber() ?? -1));
                }
                return (int)((b.ChapterNumber() ?? -1) - (a.ChapterNumber() ?? -1));
            });

            return volumeChapters.Select(chapter => new ListContentData
            {
                SubContentId = chapter.Id,
                Selected = WillBeDownloaded(chapter),
                Label = chapter.Label().Trim()
            }).ToList();
        }

        int SortFloatStrings(string a, string b)
        {
            if (a == b)
                return 0;
            
            if (string.IsNullOrEmpty(a))
                return 1;
            
            if (string.IsNullOrEmpty(b))
                return -1;
            
            float.TryParse(a, out var aFloat);
            float.TryParse(b, out var bFloat);
            return aFloat.CompareTo(bFloat);
        }
    }

    private async Task<MessageDto> FilterContent(MessageDto message)
    {
        if (State != ContentState.Ready && State != ContentState.Waiting)
            throw new MnemaException($"Cannot filter content while in the {State} state");

        _userSelectedIds = message.Data.Deserialize<List<string>>() ?? [];

        await _messageService.SizeUpdate(Request.UserId, Id, DownloadInfo.Size);
        
        return new MessageDto
        {
            Provider = provider,
            ContentId = Id,
            Type = MessageType.FilterContent,
        };
    }

    private async Task<MessageDto> StartDownload()
    {
        if (State != ContentState.Waiting)
            throw new MnemaException($"Content cannot start while in the {State} state");
        
        await _publicationManager.MoveToDownloadQueue(Id);

        return new MessageDto
        {
            Provider = provider,
            ContentId = Id,
            Type = MessageType.StartDownload,
        };
    }
    
}