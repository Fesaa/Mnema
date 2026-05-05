using System.Threading;
using System.Threading.Tasks;

namespace Mnema.API;

public interface IScheduled
{
    Task EnsureScheduledAsync(CancellationToken cancellationToken);
}
