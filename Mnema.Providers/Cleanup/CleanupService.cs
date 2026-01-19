using System;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API.Content;
using Mnema.Providers.QBit;

namespace Mnema.Providers.Cleanup;

/// <summary>
/// The general cleanup service, that decides which specific implementation to use. Registered without a key
/// </summary>
internal class CleanupService(
    TorrentCleanupService torrentCleanupService,
    PublicationCleanupService publicationCleanupService
    ): ICleanupService
{
    public async Task CleanupAsync(IContent content, CancellationToken cancellationToken = default)
    {
        switch (content)
        {
            case Publication publication:
                await publicationCleanupService.CleanupAsync(publication, cancellationToken);
                return;
            case QBitTorrent torrent:
                await torrentCleanupService.CleanupAsync(torrent, cancellationToken);
                return;
        }

        throw new ArgumentOutOfRangeException(nameof(content), $"No matching cleanup service found for {content.GetType()}");
    }
}
