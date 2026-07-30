using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public sealed record TorrentScanResult(string Size, List<Chapter> Chapters);

public sealed record ParsedTorrentInfo(string Size, List<TorrentFile> Files);

public sealed record TorrentFile(string FileName, string FilePath);

public interface IScannerService
{
    List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    Task<ParsedTorrentInfo> ParseTorrentFile(string remoteUrl, CancellationToken cancellationToken);

    T? FindMatch<T>(List<T> items, IHasPositionMarkers item) where T : IHasPositionMarkers;
}
