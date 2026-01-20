using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

[Flags]
public enum MonitoredSeriesIncludes
{
    Chapters = 0,
}

public class MonitoredSeriesRepository(MnemaDataContext ctx, IMapper mapper): IMonitoredSeriesRepository
{
    public Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(Guid userId, string query, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(m => m.UserId == userId)
            .Where(m => m.Title.Contains(query))
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .OrderBy(m => m.Id)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<MonitoredSeries?> GetMonitoredSeries(Guid id, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetMonitoredSeriesByTitle(string title, CancellationToken cancellationToken)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(m => m.ValidTitles.Any(t => t.Contains(title)))
            .ToListAsync(cancellationToken);
    }

    public Task<MonitoredSeriesDto?> GetMonitoredSeriesDto(Guid id, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetAllMonitoredSeries(CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetSeriesEligibleForRefresh(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            //.Where(s => s.LastDataRefreshUtc < cutoffDate)
            .ToListAsync(cancellationToken);
    }

    public void Update(MonitoredSeries series)
    {
        ctx.MonitoredSeries.Update(series).State = EntityState.Modified;
    }

    public void Add(MonitoredSeries series)
    {
        ctx.MonitoredSeries.Add(series).State = EntityState.Added;
    }

    public void Remove(MonitoredSeries series)
    {
        ctx.MonitoredSeries.Remove(series).State = EntityState.Deleted;
    }
}
