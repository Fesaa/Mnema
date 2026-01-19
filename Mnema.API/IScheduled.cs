using System.Threading.Tasks;

namespace Mnema.API;

public interface IScheduled
{
    Task EnsureScheduledAsync();
}
