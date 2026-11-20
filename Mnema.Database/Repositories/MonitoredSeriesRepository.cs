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
using Mnema.Models.Enums;

namespace Mnema.Database.Repositories;

public class MonitoredSeriesRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractNavigationalEntityRepository<MonitoredSeries, MonitoredSeriesDto, MonitoredSeriesIncludes>(ctx, mapper), IMonitoredSeriesRepository
{
    public Task<PagedList<MonitoredSeriesDto>> GetMonitoredSeriesDtosForUser(string query,
        Provider? provider, PaginationParams pagination,
        CancellationToken cancellationToken)
    {
        var terms = NormalizedQueryTerms(query);

        return WhereTitleMatches(ctx.MonitoredSeries, terms)
            .Includes(MonitoredSeriesIncludes.Chapters)
            .WhereIf(provider.HasValue, m => m.Provider == provider)
            .ProjectTo<MonitoredSeriesDto>(mapper.ConfigurationProvider)
            .OrderBy(m => m.NormalizedTitle)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<List<MonitoredSeriesLookupDto>> LookupMonitoredSeries(string query, int limit,
        CancellationToken cancellationToken)
    {
        var terms = NormalizedQueryTerms(query);
        if (terms.Count == 0) return Task.FromResult(new List<MonitoredSeriesLookupDto>());

        return WhereTitleMatches(ctx.MonitoredSeries, terms)
            .OrderBy(m => m.NormalizedTitle)
            .Take(Math.Clamp(limit, 1, 20))
            .ProjectTo<MonitoredSeriesLookupDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    private static List<string> NormalizedQueryTerms(string query)
    {
        return (query ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term => term.ToNormalized())
            .Where(term => !string.IsNullOrEmpty(term))
            .Distinct()
            .ToList();
    }

    private static IQueryable<MonitoredSeries> WhereTitleMatches(IQueryable<MonitoredSeries> series,
        List<string> terms)
    {
        foreach (var term in terms)
        {
            series = series.Where(m => m.NormalizedTitle.Contains(term));
        }

        return series;
    }

    public Task<List<Provider>> GetProviders(CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Select(s => s.Provider)
            .Distinct()
            .ToListAsync(cancellationToken);
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

    public Task<MonitoredSeries?> GetByMetadataIds(string hardcoverId, string mangaBakaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(hardcoverId) && string.IsNullOrEmpty(mangaBakaId))
            return Task.FromResult<MonitoredSeries?>(null);

        return ctx.MonitoredSeries
            .Where(m => (!string.IsNullOrEmpty(hardcoverId) && m.HardcoverId == hardcoverId)
                        || (!string.IsNullOrEmpty(mangaBakaId) && m.MangaBakaId == mangaBakaId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<MonitoredSeries>> GetByProvider(Provider provider, CancellationToken cancellationToken = default)
    {
        return ctx.MonitoredSeries
            .Where(s => s.Provider == provider)
            .ToListAsync(cancellationToken);
    }

    public Task<MonitoredSeries?> GetByTitle(string title, Provider provider, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title.ToNormalized();

        return ctx.MonitoredSeries
            .Where(s => s.Provider == provider && s.NormalizedTitle == normalizedTitle)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<List<MonitoredChapter>> GetUpcomingChapters(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var startOfWeek = now.AddDays(-daysSinceMonday);

        return ctx.MonitoredChapters
            .Where(c => c.Status == MonitoredChapterStatus.Upcoming
                        || (c.Status == MonitoredChapterStatus.Missing && c.ReleaseDate >= startOfWeek))
            .Include(c => c.Series)
            .ToListAsync(cancellationToken);
    }

    public Task<PagedList<MonitoredChapterDto>> GetMissingChapters(PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return ctx.MonitoredChapters
            .Where(c => c.Status == MonitoredChapterStatus.Missing ||
                        // We do the extra check for upcoming chapters, as the status isn't real time
                        // But rather only updated when the metadata is (I.e. with the hangfire task)
                        (c.Status == MonitoredChapterStatus.Upcoming && c.ReleaseDate < now))
            .Where(c => string.IsNullOrEmpty(c.Series.ExternalId))
            .ProjectTo<MonitoredChapterDto>(mapper.ConfigurationProvider)
            .OrderBy(c => c.ReleaseDate)
            .ThenBy(c => c.Title)
            .AsPagedList(pagination, cancellationToken);
    }

    public Task<bool> CheckDuplicateSeries(Guid? current, CreateOrUpdateMonitoredSeriesDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(dto.ExternalId))
        {
            return ctx.MonitoredSeries
                .Where(s => current == null || s.Id != current)
                .Where(s => s.ExternalId == dto.ExternalId)
                .AnyAsync(cancellationToken);
        }

        var normalizedTitles = dto.ValidTitles.Select(t => t.ToNormalized()).ToList();

        return ctx.MonitoredSeries
            .Where(s => current == null || s.Id != current)
            .Where(s => s.Format == dto.Format && ctx.SearchTitles
                .Where(t => t.MonitoredSeriesId == s.Id)
                .Any(t => normalizedTitles.Contains(t.NormalizedTitle)))
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
