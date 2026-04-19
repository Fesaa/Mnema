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

public class MonitoredSeriesRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractNavigationalEntityRepository<MonitoredSeries, MonitoredSeriesDto, MonitoredSeriesIncludes>(ctx, mapper), IMonitoredSeriesRepository
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
            .Where(s => !string.IsNullOrEmpty(s.HardcoverId) || !string.IsNullOrEmpty(s.MangaBakaId))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetByHardcoverIds(List<string> ids, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(s => ids.Contains(s.HardcoverId))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetByMangaBakaIds(List<string> ids, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Includes(MonitoredSeriesIncludes.Chapters)
            .Where(s => ids.Contains(s.MangaBakaId))
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetByExternalIds(List<string> ids, Provider provider, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Where(s => ids.Contains(s.ExternalId) && s.Provider == provider)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetByProvider(Provider provider, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Where(s => s.Provider == provider)
            .ToListAsync(cancellationToken);
    }

    public Task<List<MonitoredChapter>> GetUpcomingChapters(Guid userId, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredChapters
            .Where(c => c.Series.UserId == userId && c.Status == MonitoredChapterStatus.Upcoming)
            .Include(c => c.Series)
            .ToListAsync(cancellationToken);
    }

    public Task<PagedList<MonitoredChapterDto>> GetMissingChapters(Guid userId, PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return ctx.MonitoredChapters
            .Where(c => c.Series.UserId == userId)
            .Where(c => c.Status == MonitoredChapterStatus.Missing ||
                        // We do the extra check for upcoming chapters, as the status isn't real time
                        // But rather only updated when the metadata is (I.e. with the hangfire task)
                        (c.Status == MonitoredChapterStatus.Upcoming && c.ReleaseDate < now))
            .ProjectTo<MonitoredChapterDto>(mapper.ConfigurationProvider)
            .OrderBy(c => c.ReleaseDate)
            .ThenBy(c => c.Title)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<bool> CheckDuplicateSeries(Guid userId, Guid? current, CreateOrUpdateMonitoredSeriesDto dto, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(dto.ExternalId))
        {
            return ctx.MonitoredSeries
                .Where(s => current == null || s.Id != current)
                .Where(s => s.UserId == userId && s.ExternalId == dto.ExternalId)
                .AnyAsync(cancellationToken);
        }

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

    protected override IQueryable<MonitoredSeries> EntityWithIncludes(IQueryable<MonitoredSeries> query, MonitoredSeriesIncludes flags)
    {
        return query.Includes(flags);
    }
}
