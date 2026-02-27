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
    Task<bool> CheckDuplicateSeries(Guid userId, Guid? current, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);

    void RemoveRange(IEnumerable<MonitoredChapter> chapters);
}

public interface IMonitoredSeriesService
{
    public static readonly ImmutableArray<Provider> SupportedProviders = [..Enum.GetValues<Provider>()];

    Task UpdateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);
    Task CreateMonitoredSeries(Guid userId, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default);
    FormDefinition GetForm();
    Task<FormDefinition> GetMetadataForm(Guid userId, Guid seriesId, CancellationToken cancellationToken = default);

    Task EnrichWithMetadata(Guid guid, CancellationToken cancellationToken = default);
}

public static class MonitoredSeriesExtensions
{
    public static MetadataBag MetadataForDownloadRequest(this MonitoredSeries monitoredSeries)
    {
        var bag = monitoredSeries.Metadata;
        bag.SetValue(RequestConstants.HardcoverSeriesIdKey, monitoredSeries.HardcoverId);
        bag.SetValue(RequestConstants.MangaBakaKey, monitoredSeries.MangaBakaId);
        bag.SetValue(RequestConstants.ExternalIdKey, monitoredSeries.ExternalId);
        bag.SetValue(RequestConstants.FormatKey, monitoredSeries.Format.ToString());
        bag.SetValue(RequestConstants.ContentFormatKey, monitoredSeries.ContentFormat.ToString());
        bag.SetValue(RequestConstants.TitleOverride, monitoredSeries.TitleOverride);
        bag.SetValue(RequestConstants.MonitoredSeriesId, monitoredSeries.Id.ToString());

        return bag;
    }
}
