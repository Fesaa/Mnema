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
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;

namespace Mnema.Database.Repositories;

public class NotificationRepository(MnemaDataContext ctx, IMapper mapper) : INotificationRepository
{
    public Task<PagedList<NotificationDto>> GetNotificationsForUser(Guid userId, bool? read,
        PaginationParams pagination)
    {
        return ctx.Notifications
            .Where(n => n.UserId == userId && (read == null || n.Read == read))
            .ProjectTo<NotificationDto>(mapper.ConfigurationProvider)
            .OrderByDescending(n => n.CreatedUtc)
            .AsPagedList(pagination);
    }

    public Task<int> MarkNotificationsAsRead(Guid userId, IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => n.UserId == userId && ids.Contains(n.Id))
            .ExecuteUpdateAsync(n
                => n.SetProperty(p => p.Read, true));
    }

    public Task<int> MarkNotificationsAsUnRead(Guid userId, IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => n.UserId == userId && ids.Contains(n.Id))
            .ExecuteUpdateAsync(n
                => n.SetProperty(p => p.Read, false));
    }

    public Task DeleteNotifications(Guid userId, IEnumerable<Guid> ids)
    {
        return ctx.Notifications
            .Where(n => n.UserId == userId && ids.Contains(n.Id))
            .ExecuteDeleteAsync();
    }

    public Task<int> UnReadNotifications(Guid userId)
    {
        return ctx.Notifications
            .Where(n => n.UserId == userId && !n.Read)
            .CountAsync();
    }

    public void AddNotification(Notification notification)
    {
        ctx.Notifications.Add(notification).State = EntityState.Added;
    }
}
