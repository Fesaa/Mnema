using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;

namespace Mnema.Database.Repositories;

public class NotificationRepository(MnemaDataContext ctx, IMapper mapper) : AbstractEntityEntityRepository<Notification, NotificationDto>(ctx, mapper), INotificationRepository
{
    public Task<int> MarkNotificationsAsRead(IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => ids.Contains(n.Id))
            .ExecuteUpdateAsync(n
                => n.SetProperty(p => p.Read, true));
    }

    public Task<int> MarkNotificationsAsUnRead(IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => ids.Contains(n.Id))
            .ExecuteUpdateAsync(n
                => n.SetProperty(p => p.Read, false));
    }

    public Task DeleteNotifications(IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => ids.Contains(n.Id))
            .ExecuteDeleteAsync();
    }

    public Task<int> UnReadNotifications()
    {
        return ctx.Notifications
            .Where(n => !n.Read)
            .CountAsync();
    }

    public void AddNotification(Notification notification)
    {
        ctx.Notifications.Add(notification).State = EntityState.Added;
    }
}
