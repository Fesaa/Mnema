using System;
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
}

public interface IMessageService
{

    Task SizeUpdate(Guid userId, string contentId, string newSize);
    Task ProgressUpdate(Guid userId, string contentId, ContentSpeedUpdate progressSpeedUpdate);
    Task StateUpdate(Guid userId, string contentId, ContentState state);

    Task AddContent(Guid userId, DownloadInfo info);
    Task UpdateContent(Guid userId, DownloadInfo info);
    Task DeleteContent(Guid userId, string contentId);

    Task NotificationAdded(Guid userId, int amount);
    Task NotificationRemoved(Guid userId, int amount);
    Task Notify(Guid userId, NotificationDto notification);

}