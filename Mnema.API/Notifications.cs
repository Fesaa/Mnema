using Mnema.Common;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;

namespace Mnema.API;

public interface INotificationRepository
{

    Task<PagedList<NotificationDto>> GetNotificationsForUser(Guid userId, bool? read, PaginationParams pagination);

    Task<int> MarkNotificationsAsRead(Guid userId, IEnumerable<Guid> ids);
    Task<int> MarkNotificationsAsUnRead(Guid userId, IEnumerable<Guid> ids);
    Task DeleteNotifications(Guid userId, IEnumerable<Guid> ids);
    Task<int> UnReadNotifications(Guid userId);

    void AddNotification(Notification notification);

}