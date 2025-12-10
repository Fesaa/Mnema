using Microsoft.AspNetCore.Mvc;
using Mnema.Common;
using Mnema.Models.DTOs.User;

namespace Mnema.Server.Controllers;

public class NotificationsController: BaseApiController
{

    [HttpGet("all")]
    public async Task<ActionResult<IList<NotificationDto>>> GetNotifications([FromQuery] DateTime? after, [FromQuery] PaginationParams pagination)
    {
        return Ok(new List<NotificationDto>());
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IList<NotificationDto>>> GetRecentNotifications([FromQuery] int limit)
    {
        return Ok(new List<NotificationDto>());
    }

    [HttpGet("amount")]
    public async Task<ActionResult<int>> AmountOfUnread()
    {
        return Ok(0);
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> ReadNotification(Guid notificationId)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("{notificationId:guid}/unread")]
    public async Task<IActionResult> UnReadNotification(Guid notificationId)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{notificationId:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid notificationId)
    {
        throw new NotImplementedException();
    }

    [HttpPost("many")]
    public async Task<IActionResult> ReadMany([FromBody] int[] ids)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("many/delete")]
    public async Task<IActionResult> DeleteMany([FromBody] int[] ids)
    {
        throw new NotImplementedException();
    }
    
    
    
}