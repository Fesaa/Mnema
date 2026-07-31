using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.User;

namespace Mnema.API;

public enum MessageEventType
{
    ContentInfoUpdate,
    ContentSizeUpdate,
    ContentProgressUpdate,
    AddContent,
    DeleteContent,
    ContentStateUpdate,
    Notification,
    NotificationRead,
    NotificationAdd,
    BulkContentInfoUpdate,
    MetadataRefreshed,
    RefreshDashboard,
}

public interface IMessageService
{
    Task SizeUpdate(string contentId, string newSize);
    Task ProgressUpdate(string contentId, ContentSpeedUpdate progressSpeedUpdate);
    Task StateUpdate(string contentId, ContentState state);

    Task AddContent(DownloadInfo info);
    Task UpdateContent(DownloadInfo info);
    Task BulkContentInfoUpdate(DownloadInfo[] downloadInfos);
    Task DeleteContent(string contentId);
    Task RefreshDashboard();

    Task NotificationAdded(int amount);
    Task NotificationRemoved(int amount);
    Task Notify(NotificationDto notification);

    Task MetadataRefreshed(Guid seriesId);
}
