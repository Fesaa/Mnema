using System;
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
    public async Task Cleanup(IContent content)
    {
        switch (content)
        {
            case Publication publication:
                await publicationCleanupService.Cleanup(publication);
                return;
            case QBitTorrent torrent:
                await torrentCleanupService.Cleanup(torrent);
                return;
        }

        throw new ArgumentOutOfRangeException(nameof(content), $"No matching cleanup service found for {content.GetType()}");
    }
}
