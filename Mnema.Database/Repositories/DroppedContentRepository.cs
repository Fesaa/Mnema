using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.API.Repositories;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class DroppedContentRepository(MnemaDataContext ctx)
    : AbstractDbOnlyEntityRepository<DroppedContent>(ctx), IDroppedContentRepository
{
    public Task<bool> ExistsByMonitoredSeriesIdAsync(Guid monitoredSeriesId, CancellationToken cancellationToken)
    {
        return ctx.DroppedContent.AnyAsync(dc => dc.MonitoredSeriesId == monitoredSeriesId, cancellationToken);
    }
}
