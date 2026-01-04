using System;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Models.Entities.Content;

public class Subscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public MnemaUser User { get; set; }

    /// <summary>
    ///     The external content id
    /// </summary>
    public required string ContentId { get; set; }

    /// <summary>
    ///     Title given by the user, defaults to the series name
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///     The directory to download the content in
    /// </summary>
    public required string BaseDir { get; set; }

    public required Provider Provider { get; set; }
    public required DownloadMetadataDto Metadata { get; set; }

    /// <summary>
    ///     When the last run took place
    /// </summary>
    public DateTime LastRun { get; set; }

    /// <summary>
    ///     If the last run was a success
    /// </summary>
    public bool LastRunSuccess { get; set; }

    /// <summary>
    ///     When the next run is expected to take place
    /// </summary>
    public DateTime NextRun { get; set; }

    /// <summary>
    ///     Represents the amount of sequential runs without any chapters being downloaded
    /// </summary>
    public int NoDownloadsRuns { get; set; }

    /// <summary>
    ///     How often to check for updates
    /// </summary>
    public RefreshFrequency RefreshFrequency { get; set; }

    private static DateTime NormalizeLocalHourToUtc(DateTime utcNow, int hour)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.Local);

        var localTarget = new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            hour,
            0,
            0,
            DateTimeKind.Unspecified
        );

        return TimeZoneInfo.ConvertTimeToUtc(localTarget, TimeZoneInfo.Local);
    }


    public DateTime NextRunTime(int hour)
    {
        var nowUtc = DateTime.UtcNow;
        var diff = nowUtc - LastRun; // LastRun MUST be UTC

        DateTime nextUtc;

        if (diff > RefreshFrequency.AsTimeSpan())
        {
            nextUtc = NormalizeLocalHourToUtc(nowUtc, hour);

            if (nextUtc <= nowUtc) nextUtc = NormalizeLocalHourToUtc(nowUtc.AddDays(1), hour);

            return nextUtc;
        }

        nextUtc = nowUtc.Add(RefreshFrequency.AsTimeSpan() - diff);
        nextUtc = NormalizeLocalHourToUtc(nextUtc, hour);

        if (nextUtc <= nowUtc) nextUtc = NormalizeLocalHourToUtc(nextUtc.AddDays(1), hour);

        return nextUtc;
    }

    public DownloadRequestDto AsDownloadRequestDto()
    {
        return new DownloadRequestDto
        {
            Provider = Provider,
            Id = ContentId,
            BaseDir = BaseDir,
            TempTitle = Title,
            DownloadMetadata = Metadata,
            UserId = UserId,
            SubscriptionId = Id
        };
    }
}

public enum RefreshFrequency
{
    Day = 2,
    Week = 3,
    Month = 4
}

public static class SubscriptionExtensions
{
    public static TimeSpan AsTimeSpan(this RefreshFrequency refreshFrequency)
    {
        return refreshFrequency switch
        {
            RefreshFrequency.Day => TimeSpan.FromDays(1),
            RefreshFrequency.Week => TimeSpan.FromDays(7),
            RefreshFrequency.Month => TimeSpan.FromDays(30),
            _ => throw new ArgumentOutOfRangeException(nameof(refreshFrequency), refreshFrequency, null)
        };
    }

    public static DateTime NormalizeToHour(this DateTime date, int hour)
    {
        return DateOnly.FromDateTime(date)
            .ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(hour)), DateTimeKind.Local);
    }
}