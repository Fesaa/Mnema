using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;

namespace Mnema.Services.Hubs;

internal class MessageService(IHubContext<MessageHub> ctx) : IMessageService
{
    public async Task SizeUpdate(Guid userId, string contentId, string newSize)
    {
        await SendToUser(userId, nameof(MessageEventType.ContentSizeUpdate), new ContentSizeUpdate
        {
            ContentId = contentId,
            Size = newSize
        });
    }

    public async Task ProgressUpdate(Guid userId, string contentId, ContentSpeedUpdate progressSpeedUpdate)
    {
        await SendToUser(userId, nameof(MessageEventType.ContentProgressUpdate), progressSpeedUpdate);
    }

    public async Task StateUpdate(Guid userId, string contentId, ContentState state)
    {
        await SendToUser(userId, nameof(MessageEventType.ContentStateUpdate), new ContentStateUpdate
        {
            ContentId = contentId,
            ContentState = state
        });
    }

    public async Task AddContent(Guid userId, DownloadInfo info)
    {
        await SendToUser(userId, nameof(MessageEventType.AddContent), info);
    }

    public async Task UpdateContent(Guid userId, DownloadInfo info)
    {
        await SendToUser(userId, nameof(MessageEventType.ContentInfoUpdate), info);
    }

    public async Task BulkContentInfoUpdate(Guid userId, DownloadInfo[] downloadInfos)
    {
        await SendToUser(userId, nameof(MessageEventType.BulkContentInfoUpdate), downloadInfos);
    }

    public async Task DeleteContent(Guid userId, string contentId)
    {
        await SendToUser(userId, nameof(MessageEventType.DeleteContent), new ContentUpdate
        {
            ContentId = contentId
        });
    }

    public async Task NotificationAdded(Guid userId, int amount)
    {
        await SendToUser(userId, nameof(MessageEventType.NotificationAdd), new
        {
            Amount = amount
        });
    }

    public async Task NotificationRemoved(Guid userId, int amount)
    {
        await SendToUser(userId, nameof(MessageEventType.NotificationRead), new
        {
            Amount = amount
        });
    }

    public async Task Notify(Guid userId, NotificationDto notification)
    {
        await SendToUser(userId, nameof(MessageEventType.Notification), notification);
        await SendToUser(userId, nameof(MessageEventType.NotificationAdd));
    }

    public async Task MetadataRefreshed(Guid userId, Guid seriesId)
    {
        await SendToUser(userId, nameof(MessageEventType.MetadataRefreshed), new
        {
            SeriesId = seriesId
        });
    }

    private async Task SendToUser(Guid userId, string method, object? body = null)
    {
        await ctx.Clients.User(userId.ToString()).SendAsync(method, body);
    }
}
