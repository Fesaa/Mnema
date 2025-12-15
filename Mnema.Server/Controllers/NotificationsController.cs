using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.User;

namespace Mnema.Server.Controllers;

public class NotificationsController(IUnitOfWork unitOfWork): BaseApiController
{

    [HttpGet("all")]
    public async Task<ActionResult<IList<NotificationDto>>> GetNotifications([FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;

        var notifications = await unitOfWork.NotificationRepository.GetNotificationsForUser(UserId, pagination);

        return Ok(notifications);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IList<NotificationDto>>> GetRecentNotifications([FromQuery] int limit)
    {
        var notifications = await unitOfWork.NotificationRepository.GetNotificationsForUser(UserId, new PaginationParams
        {
            PageNumber = 0,
            PageSize = limit,
        });
        
        return Ok(notifications.Items);
    }

    [HttpGet("amount")]
    public async Task<ActionResult<int>> AmountOfUnread()
    {
        return Ok(await unitOfWork.NotificationRepository.UnReadNotifications(UserId));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> ReadNotification(Guid notificationId)
    {
        await unitOfWork.NotificationRepository.MarkNotificationsAsRead(UserId, [notificationId]);

        return Ok();
    }
    
    [HttpPost("{notificationId:guid}/unread")]
    public async Task<IActionResult> UnReadNotification(Guid notificationId)
    {
        await unitOfWork.NotificationRepository.MarkNotificationsAsUnRead(UserId, [notificationId]);

        return Ok();
    }

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        await unitOfWork.NotificationRepository.DeleteNotifications(UserId, [notificationId]);

        return Ok();
    }

    [HttpPost("many/read")]
    public async Task<IActionResult> ReadMany([FromBody] Guid[] ids)
    {
        await unitOfWork.NotificationRepository.MarkNotificationsAsRead(UserId, ids);

        return Ok();
    }
    
    [HttpPost("many/unread")]
    public async Task<IActionResult> UnReadMany([FromBody] Guid[] ids)
    {
        await unitOfWork.NotificationRepository.MarkNotificationsAsRead(UserId, ids);

        return Ok();
    }
    
    [HttpPost("many/delete")]
    public async Task<IActionResult> DeleteMany([FromBody] Guid[] ids)
    {
        await unitOfWork.NotificationRepository.DeleteNotifications(UserId, ids);

        return Ok();
    }
    
    
    
}