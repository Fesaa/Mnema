using System.Threading.Tasks;
using Mnema.API.Content;

namespace Mnema.Providers.Cleanup;

internal class TorrentCleanupService: ICleanupService
{
    public Task Cleanup(IContent content)
    {
        throw new System.NotImplementedException();
    }
}
