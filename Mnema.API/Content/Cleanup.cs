using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface ICleanupService
{
    Task CleanupAsync(IContent content, CancellationToken cancellationToken = default);
}
