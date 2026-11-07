using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface ICoverService
{
    Task<List<Cover>> GetCovers(MonitoredSeries series, CancellationToken cancellationToken = default);

    Task<ArchiveCover?> GetArchiveCover(MonitoredChapter chapter, CancellationToken cancellationToken = default);

    Task UpdateCovers(MonitoredSeries series, IList<MonitoredChapter> chapters, string coverUrl,
        CancellationToken cancellationToken = default);
}
