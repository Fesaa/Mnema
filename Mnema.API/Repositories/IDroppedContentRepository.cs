using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Repositories;

public interface IDroppedContentRepository : IDbOnlyEntityRepository<DroppedContent>
{
    Task<bool> ExistsByMonitoredSeriesIdAsync(Guid monitoredSeriesId, CancellationToken cancellationToken);
}
