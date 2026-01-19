using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QBittorrent.Client;

namespace Mnema.Providers.QBit;

internal interface IQBitClient : IDisposable
{
    Task<IReadOnlyList<TorrentInfo>> GetTorrentsAsync(TorrentListQuery? query = null, CancellationToken token = default);
    Task AddTorrentsAsync(AddTorrentUrlsRequest request, CancellationToken token = default);
    Task DeleteTorrentsAsync(IEnumerable<string> hashes, bool deleteFiles, CancellationToken token = default);
    Task<IReadOnlyList<TorrentContent>> GetTorrentContentsAsync(string hash, CancellationToken token = default);
    Task SetFilePriorityAsync(string hash, IEnumerable<int> indices, TorrentContentPriority priority, CancellationToken token = default);
    Task ResumeTorrentsAsync(IEnumerable<string> hashes, CancellationToken token = default);
    void Invalidate();
}
