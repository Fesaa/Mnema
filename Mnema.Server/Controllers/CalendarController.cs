using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Mnema.API;

namespace Mnema.Server.Controllers;

public class CalendarController(ICalendarService calendarService, IDistributedCache cache) : BaseApiController
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
}
