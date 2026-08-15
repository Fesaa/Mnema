using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Extensions;

namespace Mnema.Services;

public class CalendarService(IUnitOfWork unitOfWork, INamingService namingService): ICalendarService
{
    private static readonly CalendarSerializer CalendarSerializer = new();

    public async Task<string> CreateCalendar(CancellationToken cancellationToken)
    {
        var preferences = await unitOfWork.SettingsRepository.GetPreferencesAsync(cancellationToken);

        var upcomingChapters =
            await unitOfWork.MonitoredSeriesRepository.GetUpcomingChapters(cancellationToken);

        var events = upcomingChapters.Select(c =>
        {
            if (c.ReleaseDate == null) return null;

            var localRelease = c.ReleaseDate.Value.ToLocalTime();
            var date = new CalDateTime(localRelease.Year, localRelease.Month, localRelease.Day);

            return new CalendarEvent
            {
                Summary = namingService.GetChapterFileName(preferences, c.Series.Title, c.AsChapter()),
                Description = !string.IsNullOrEmpty(c.Summary) ? c.Summary : c.Series.Summary,
                Url = SafeUri(c.RefUrl),
                Start = date,
                End = date.AddDays(1)
            };
        }).WhereNotNull();

        var calendar = new Calendar();
        calendar.Events.AddRange(events);
        calendar.AddTimeZone(TimeZoneInfo.Local);

        return CalendarSerializer.SerializeToString(calendar) ?? string.Empty;
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
