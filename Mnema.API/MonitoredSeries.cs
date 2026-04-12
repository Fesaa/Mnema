using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

[Flags]
public enum MonitoredSeriesIncludes
{
    Chapters = 0,
}

public interface IMonitoredSeriesRepository: INavigationalEntityRepository<MonitoredSeries, MonitoredSeriesDto, MonitoredSeriesIncludes>
{
    Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(Guid userId, string query, Provider? provider, PaginationParams pagination, CancellationToken cancellationToken);
    Task<List<MonitoredSeries>> GetSeriesEligibleForRefresh(CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetByHardcoverIds(List<string> ids, CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetByMangaBakaIds(List<string> ids, CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetByExternalIds(List<string> ids, Provider provider, CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetByProvider(Provider provider, CancellationToken cancellationToken = default);
    Task<List<MonitoredChapter>> GetUpcomingChapters(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CheckDuplicateSeries(Guid userId, Guid? current, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<MonitoredChapter> chapters);
}

public interface IMonitoredSeriesService
{
    public static readonly ImmutableArray<Provider> SupportedProviders = [..Enum.GetValues<Provider>()];

    Task UpdateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);
    Task CreateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);
    FormDefinition GetForm();
    Task<FormDefinition> GetMetadataForm(Guid userId, Provider provider, CancellationToken cancellationToken = default);

    Task EnrichWithMetadata(Guid guid, CancellationToken cancellationToken = default, bool firstRun = false);
    Task StartDownload(Guid userId, Guid seriesId, bool firstDownload, CancellationToken ct = default);
}

public static class MonitoredSeriesExtensions
{
    public static MetadataBag MetadataForDownloadRequest(this MonitoredSeries monitoredSeries)
    {
        var bag = monitoredSeries.Metadata;
        bag.SetKey(RequestConstants.HardcoverSeriesIdKey, monitoredSeries.HardcoverId);
        bag.SetKey(RequestConstants.MangaBakaKey, monitoredSeries.MangaBakaId);
        bag.SetKey(RequestConstants.ExternalIdKey, monitoredSeries.ExternalId);
        bag.SetKey(RequestConstants.FormatKey, monitoredSeries.Format);
        bag.SetKey(RequestConstants.ContentFormatKey, monitoredSeries.ContentFormat);
        bag.SetKey(RequestConstants.TitleOverride, monitoredSeries.TitleOverride);
        bag.SetKey(RequestConstants.MonitoredSeriesId, monitoredSeries.Id);
        bag.SetIfNotPresent(RequestConstants.IgnoreNonMatchedVolumes, true);

        return bag;
    }
}
