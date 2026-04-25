using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mnema.Providers.Managers.Publication;

internal interface IPreDownloadHook
{
    Task PreDownloadHook(Publication publication, IServiceScope scope, CancellationToken cancellationToken);
}

internal interface IIoHandler
{
    Task HandleIoWork(string title, string id, IoWork ioWork, CancellationTokenSource tokenSource);
}
