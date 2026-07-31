using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities;

namespace Mnema.API;

public interface INotificationRepository: IEntityRepository<Notification, NotificationDto>
{
    Task<int> MarkNotificationsAsRead(IEnumerable<Guid> ids);
    Task<int> MarkNotificationsAsUnRead(IEnumerable<Guid> ids);
    Task DeleteNotifications(IEnumerable<Guid> ids);
    Task<int> UnReadNotifications();

    void AddNotification(Notification notification);
}
