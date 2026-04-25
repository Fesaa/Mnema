using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Managers.Publication;

namespace Mnema.Providers.Cleanup;

internal class PublicationCleanupService(RawFileCleanupService fileCleanupService): ICleanupService
{
    public Task CleanupAsync(IContent content, CancellationToken cancellationToken = default)
    {
        if (content is not Publication publication)
            throw new MnemaException($"{nameof(PublicationCleanupService)} cannot cleanup {content.GetType()}");

        if (publication.Request.Provider.IsDirectDownload())
        {
            return fileCleanupService.CleanupAsync(publication, cancellationToken);
        }

        return publication.Cleanup();
    }
}
