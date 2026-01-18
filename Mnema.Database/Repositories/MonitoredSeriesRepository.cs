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

public class MonitoredSeriesRepository(MnemaDataContext ctx, IMapper mapper): IMonitoredSeriesRepository
{
    public Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(Guid userId, string query, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        return ctx.MonitoredSeries
            .Where(m => m.UserId == userId)
            .Where(m => m.Title.Contains(query))
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .OrderBy(m => m.Id)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<MonitoredSeries?> GetMonitoredSeries(Guid id, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetMonitoredSeriesByTitle(string title, CancellationToken cancellationToken)
    {
        return ctx.MonitoredSeries
            .Where(m => m.ValidTitles.Any(t => t.Contains(title)))
            .ToListAsync(cancellationToken);
    }

    public Task<MonitoredSeriesDto?> GetMonitoredSeriesDto(Guid id, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetAllMonitoredSeries(CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries.ToListAsync(cancellationToken);
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
