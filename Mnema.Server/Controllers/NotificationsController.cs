using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Server.Controllers;

public class NotificationsController(IUnitOfWork unitOfWork, IMessageService messageService) : BaseApiController
{
    [HttpGet("enabled")]
    public async Task<ActionResult<bool>> Enabled()
    {
        return Ok(await unitOfWork.ConnectionRepository.ConnectionExistsForType(ConnectionType.Native, HttpContext.RequestAborted));
    }

    [HttpGet("all")]
    public async Task<ActionResult<IList<NotificationDto>>> GetNotifications([FromQuery] PaginationParams? pagination)
    {
        pagination ??= PaginationParams.Default;

        var notifications = await unitOfWork.NotificationRepository.GetAllPaged(pagination, HttpContext.RequestAborted);

        return Ok(notifications);
    }

    [HttpGet("amount")]
    public async Task<ActionResult<int>> AmountOfUnread()
    {
        return Ok(await unitOfWork.NotificationRepository.UnReadNotifications());
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> ReadNotification(Guid notificationId)
    {
        var changes = await unitOfWork.NotificationRepository.MarkNotificationsAsRead([notificationId]);

        if (changes > 0) await messageService.NotificationRemoved(changes);

        return Ok();
    }

    [HttpPost("{notificationId:guid}/unread")]
    public async Task<IActionResult> UnReadNotification(Guid notificationId)
    {
        var changes = await unitOfWork.NotificationRepository.MarkNotificationsAsUnRead([notificationId]);

        if (changes > 0) await messageService.NotificationAdded(changes);

        return Ok();
    }

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        await unitOfWork.NotificationRepository.DeleteNotifications([notificationId]);

        return Ok();
    }

    [HttpPost("many/read")]
    public async Task<IActionResult> ReadMany([FromBody] Guid[] ids)
    {
        var changes = await unitOfWork.NotificationRepository.MarkNotificationsAsRead(ids);

        if (changes > 0) await messageService.NotificationRemoved(changes);

        return Ok();
    }

    [HttpPost("many/unread")]
    public async Task<IActionResult> UnReadMany([FromBody] Guid[] ids)
    {
        var changes = await unitOfWork.NotificationRepository.MarkNotificationsAsRead(ids);

        if (changes > 0) await messageService.NotificationAdded(changes);

        return Ok();
    }

    [HttpPost("many/delete")]
    public async Task<IActionResult> DeleteMany([FromBody] Guid[] ids)
    {
        await unitOfWork.NotificationRepository.DeleteNotifications(ids);

        return Ok();
    }
}
