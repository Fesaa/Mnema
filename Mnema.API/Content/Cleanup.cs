using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.Content;

namespace Mnema.API.Content;

public interface ICleanupService
{

    public const string RawFileCleanupServiceKey = nameof(RawFileCleanupServiceKey);

    Task CleanupAsync(IContent content, CancellationToken cancellationToken = default);
}
