using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

public class ContentReleaseRepository(MnemaDataContext ctx, IMapper mapper) : BaseContentReleaseRepository (
    ctx,
    mapper,
    r => r.Type == ReleaseType.Processed
);

public class ImportedContentReleaseRepository(MnemaDataContext ctx, IMapper mapper) : BaseContentReleaseRepository (
    ctx,
    mapper,
    r => r.Type == ReleaseType.Imported
);

public abstract class BaseContentReleaseRepository(MnemaDataContext ctx, IMapper mapper, Expression<Func<ContentRelease, bool>> filter): IContentReleaseRepository
{

    public Task<PagedList<ContentReleaseDto>> GetReleases(PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        return ctx.ContentReleases
            .Where(filter)
            .ProjectTo<ContentReleaseDto>(mapper.ConfigurationProvider)
            .OrderBy(r => r.Id)
            .AsPagedList(paginationParams, cancellationToken);
    }

    public Task<List<ContentRelease>> GetReleasesSince(DateTime since, CancellationToken cancellationToken = default)
    {
        return ctx.ContentReleases
            .Where(filter)
            .Where(r => r.ReleaseDate >= since)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<string>> FilterReleases(List<string> releaseIds, CancellationToken cancellationToken = default)
    {
        var idsOnDatabase = await ctx.ContentReleases
            .Where(filter)
            .Where(r => releaseIds.Contains(r.ReleaseId))
            .Select(r => r.ReleaseId)
            .ToListAsync(cancellationToken);

        return releaseIds.Except(idsOnDatabase).ToHashSet();
    }

    public void AddRange(ICollection<ContentRelease> releases)
    {
        ctx.ContentReleases.AddRange(releases);
    }
}
