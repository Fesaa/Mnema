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
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public void Add(ContentRelease release)
    {
        ctx.ProcessedContentReleases.Add(release).State = EntityState.Added;
    }
}
