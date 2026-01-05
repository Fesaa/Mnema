using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IContentReleaseRepository
{
    Task<PagedList<ContentReleaseDto>> GetReleases(PaginationParams paginationParams, CancellationToken cancellationToken);
    Task<List<ContentRelease>> GetReleasesSince(DateTime since, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the ids that are not already present in the database
    /// </summary>
    /// <param name="releaseIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<HashSet<string>> FilterReleases(List<string> releaseIds, CancellationToken cancellationToken = default);

    void AddRange(ICollection<ContentRelease> release);
}
