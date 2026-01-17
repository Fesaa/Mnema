using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mnema.Providers;

internal interface IPreDownloadHook
{
    Task PreDownloadHook(Publication publication, IServiceScope scope, CancellationToken cancellationToken);
}
