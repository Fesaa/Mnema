using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IContentReleaseRepository
{
    Task<List<ContentRelease>> GetReleasesSince(DateTime since, CancellationToken cancellationToken = default);

    void Add(ContentRelease release);
}
