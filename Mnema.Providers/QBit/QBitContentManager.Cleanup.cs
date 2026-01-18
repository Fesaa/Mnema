using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Providers.QBit;

internal partial class QBitContentManager
{

    private readonly ConcurrentDictionary<string, bool> _cleanupTorrents = [];

    private void CleanupTorrent(QBitTorrent torrent)
    {
        if (!_cleanupTorrents.TryAdd(torrent.Id, true)) return;

        Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var sw = Stopwatch.StartNew();

            try
            {
                var cleanupService = scope.ServiceProvider.GetRequiredService<ICleanupService>();
                await cleanupService.Cleanup(torrent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to cleanup torrent {TorrentId}", torrent.Id);
            }
            finally
            {
                _cleanupTorrents.TryRemove(torrent.Id, out _);

                unitOfWork.ImportedReleaseRepository.AddRange([
                    new ContentRelease
                    {
                        ReleaseId = torrent.Id,
                        ReleaseName = torrent.Title,
                        ContentName = torrent.Title,
                        Type = ReleaseType.Imported,
                        ReleaseDate = DateTime.UtcNow
                    }
                ]);

                await unitOfWork.CommitAsync();

                logger.LogInformation("[{Title}/{Id}] Cleaned up in {Elapsed}ms",  torrent.Title, torrent.Id, sw.ElapsedMilliseconds);
            }
        });
    }

}
