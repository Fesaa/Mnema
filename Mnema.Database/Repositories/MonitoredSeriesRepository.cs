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
using Mnema.Common.Extensions;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

[Flags]
public enum MonitoredSeriesIncludes
{
    Chapters = 0,
}

public class MonitoredSeriesRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractEntityEntityRepository<MonitoredSeries, MonitoredSeriesDto>(ctx, mapper), IMonitoredSeriesRepository
{
    public Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(Guid userId, string query,
        Provider? provider, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        query = query.ToNormalized();

        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(m => m.UserId == userId)
            .Where(m => m.NormalizedTitle.Contains(query))
            .WhereIf(provider.HasValue, m => m.Provider == provider)
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .OrderBy(m => m.Id)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetSeriesEligibleForRefresh(CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);

        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(s => s.LastDataRefreshUtc < cutoffDate)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> CheckDuplicateSeries(Guid userId, Guid? current, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Where(s => current == null || s.Id != current)
            .WhereIf(!string.IsNullOrEmpty(dto.HardcoverId), s => s.HardcoverId == dto.HardcoverId)
            .WhereIf(!string.IsNullOrEmpty(dto.MangaBakaId), s => s.MangaBakaId == dto.MangaBakaId)
            .Where(s => s.Format == dto.Format && s.ValidTitles.Intersect(dto.ValidTitles).Any())
            .AnyAsync(cancellationToken);
    }

    public void RemoveRange(IEnumerable<MonitoredChapter> chapters)
    {
        ctx.MonitoredChapters.RemoveRange(chapters);
    }
}
