using System.Threading;
using System.Threading.Tasks;

namespace Mnema.API.Content;

public interface ICleanupService
{
    Task CleanupAsync(IContent content, CancellationToken cancellationToken = default);
}
