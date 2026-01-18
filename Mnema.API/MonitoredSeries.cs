using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IMonitoredSeriesScheduler
{
    Task EnsureScheduledAsync();
}

public interface IMonitoredSeriesRepository
{
    Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(Guid userId, string query, PaginationParams pagination, CancellationToken cancellationToken);
    Task<MonitoredSeries?> GetMonitoredSeries(Guid id, CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetMonitoredSeriesByTitle(string title, CancellationToken cancellationToken);
    Task<MonitoredSeriesDto?> GetMonitoredSeriesDto(Guid id, CancellationToken cancellationToken = default);
    Task<List<MonitoredSeries>> GetAllMonitoredSeries(CancellationToken cancellationToken = default);

    void Update(MonitoredSeries series);
    void Add(MonitoredSeries series);
    void Remove(MonitoredSeries series);
}

public interface IMonitoredSeriesService
{
    Task<bool> DownloadFromRelease(MonitoredSeries series, ContentRelease release, CancellationToken cancellationToken = default);
}
