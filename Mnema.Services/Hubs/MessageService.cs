using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;

namespace Mnema.Services.Hubs;

internal class MessageService(IHubContext<MessageHub> ctx) : IMessageService
{
    public async Task SizeUpdate(string contentId, string newSize)
    {
        await Send(nameof(MessageEventType.ContentSizeUpdate), new ContentSizeUpdate
        {
            ContentId = contentId,
            Size = newSize
        });
    }

    public async Task ProgressUpdate(string contentId, ContentSpeedUpdate progressSpeedUpdate)
    {
        await Send(nameof(MessageEventType.ContentProgressUpdate), progressSpeedUpdate);
    }

    public async Task StateUpdate(string contentId, ContentState state)
    {
        await Send(nameof(MessageEventType.ContentStateUpdate), new ContentStateUpdate
        {
            ContentId = contentId,
            ContentState = state
        });
    }

    public async Task AddContent(DownloadInfo info)
    {
        await Send(nameof(MessageEventType.AddContent), info);
    }

    public async Task UpdateContent(DownloadInfo info)
    {
        await Send(nameof(MessageEventType.ContentInfoUpdate), info);
    }

    public async Task BulkContentInfoUpdate(DownloadInfo[] downloadInfos)
    {
        await Send(nameof(MessageEventType.BulkContentInfoUpdate), downloadInfos);
    }

    public async Task DeleteContent(string contentId)
    {
        await Send(nameof(MessageEventType.DeleteContent), new ContentUpdate
        {
            ContentId = contentId
        });
    }

    public async Task RefreshDashboard()
    {
        await Send(nameof(MessageEventType.RefreshDashboard));
    }

    public async Task NotificationAdded(int amount)
    {
        await Send(nameof(MessageEventType.NotificationAdd), new
        {
            Amount = amount
        });
    }

    public async Task NotificationRemoved(int amount)
    {
        await Send(nameof(MessageEventType.NotificationRead), new
        {
            Amount = amount
        });
    }

    public async Task Notify(NotificationDto notification)
    {
        await Send(nameof(MessageEventType.Notification), notification);
        await Send(nameof(MessageEventType.NotificationAdd));
    }

    public async Task MetadataRefreshed(Guid seriesId)
    {
        await Send(nameof(MessageEventType.MetadataRefreshed), new
        {
            SeriesId = seriesId
        });
    }

    private async Task Send(string method, object? body = null)
    {
        await ctx.Clients.All.SendAsync(method, body);
    }
}
