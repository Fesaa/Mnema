using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class ContentReleaseRepository(MnemaDataContext ctx, IMapper mapper): IContentReleaseRepository
{
    public Task<List<ContentRelease>> GetReleasesSince(DateTime since, CancellationToken cancellationToken = default)
    {
        return ctx.ProcessedContentReleases
            .Where(r => r.ReleaseDate >= since)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<string>> FilterReleases(List<string> releaseIds, CancellationToken cancellationToken = default)
    {
        var idsOnDatabase = await ctx.ProcessedContentReleases
            .Where(r => releaseIds.Contains(r.ReleaseId))
            .Select(r => r.ReleaseId)
            .ToListAsync(cancellationToken);

        return releaseIds.Except(idsOnDatabase).ToHashSet();
    }

    public void AddRange(ICollection<ContentRelease> releases)
    {
        ctx.ProcessedContentReleases.AddRange(releases);
    }
}
