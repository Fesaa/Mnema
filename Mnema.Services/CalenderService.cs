using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

public class CalendarService(IUnitOfWork unitOfWork): ICalendarService
{
    private static readonly CalendarSerializer CalendarSerializer = new();

    public async Task<string> CreateCalendar(Guid userId, CancellationToken cancellationToken)
    {
        var upcomingChapters =
            await unitOfWork.MonitoredSeriesRepository.GetUpcomingChapters(userId, cancellationToken);

        var events = upcomingChapters.Select(c =>
        {
            if (c.ReleaseDate == null) return null;
            var releaseDate = c.ReleaseDate.Value;

            var date = new CalDateTime(releaseDate.Year, releaseDate.Month, releaseDate.Day);
            return new CalendarEvent
            {
                Summary = CalendarEventName(c),
                Description = !string.IsNullOrEmpty(c.Summary) ? c.Summary : c.Series.Summary,
                Url = SafeUri(c.RefUrl),
                Start = date,
                End = date.AddDays(1)
            };
        }).WhereNotNull();

        var calendar = new Ical.Net.Calendar();
        calendar.Events.AddRange(events);
        calendar.AddTimeZone(TimeZoneInfo.Local);

        return CalendarSerializer.SerializeToString(calendar) ?? string.Empty;
    }

    private static string CalendarEventName(MonitoredChapter chapter)
    {
        var title = chapter.Title;

        if (!string.IsNullOrEmpty(chapter.Volume) && !title.Contains("Vol."))
            title += $" Vol. {chapter.Volume}";

        if (!string.IsNullOrEmpty(chapter.Chapter) && !title.Contains("Ch."))
            title += $" Ch. {chapter.Chapter}";

        return title;
    }

    private static Uri? SafeUri(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            return new Uri(url);
        }
        catch
        {
            return null;
        }
    }
}
