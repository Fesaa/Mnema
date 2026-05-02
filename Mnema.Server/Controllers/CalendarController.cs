using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.User;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize(Roles = Roles.Calendar)]
public class CalendarController(ILogger<CalendarController> logger, IUnitOfWork unitOfWork,
    IAuthKeyService authKeyService, ICalendarService calendarService, IDistributedCache cache,
    ApplicationConfiguration applicationConfiguration) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> GetCalendar()
    {
        var cacheKey = $"calendar_{UserId}";
        byte[] calendarBytes;

        var cachedCalendar = await cache.GetAsync(cacheKey, HttpContext.RequestAborted);

        if (cachedCalendar != null)
        {
            calendarBytes = cachedCalendar;
        }
        else
        {
            var calendarString = await calendarService.CreateCalendar(UserId, HttpContext.RequestAborted);
            calendarBytes = Encoding.UTF8.GetBytes(calendarString);

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            };

            await cache.SetAsync(cacheKey, calendarBytes, cacheOptions, HttpContext.RequestAborted);
        }

        return File(calendarBytes, "text/calendar", "calendar.ics");
    }

    [HttpGet("url")]
    public async Task<ActionResult<string>> GetCalendarString()
    {
        var authKey = await unitOfWork.AuthKeyRepository.GetAuthKeyForUser(UserId, [Roles.Calendar], HttpContext.RequestAborted);
        if (authKey == null)
        {
            logger.LogWarning("No auth key found for user {UserId} with the Calendar permission creating one", UserId);
            await authKeyService.CreateAuthKey(UserId, new AuthKeyDto
            {
                Name = "Calendar Key",
                Roles = [Roles.Calendar],
                Key = Guid.NewGuid().ToString()
            }, User, HttpContext.RequestAborted);

            authKey = await unitOfWork.AuthKeyRepository.GetAuthKeyForUser(UserId, [Roles.Calendar], HttpContext.RequestAborted);
        }

        if (authKey == null)
        {
            throw new BadRequestException("Failed to retrieve any authkey for user");
        }

        if (string.IsNullOrEmpty(applicationConfiguration.Host))
        {
            throw new BadRequestException("No host configured, cannot auto generate calender url");
        }

        return Ok($"{applicationConfiguration.Host.Trim('/')}/api/calendar?authkey={authKey.Key}");
    }
}
