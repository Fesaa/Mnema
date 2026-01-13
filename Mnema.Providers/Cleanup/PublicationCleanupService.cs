using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common.Exceptions;

namespace Mnema.Providers.Cleanup;

internal class PublicationCleanupService: ICleanupService
{
    public Task Cleanup(IContent content)
    {
        if (content is not Publication publication)
            throw new MnemaException($"{nameof(PublicationCleanupService)} cannot cleanup {content.GetType()}");

        return publication.Cleanup();
    }
}
