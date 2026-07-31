using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;

namespace Mnema.API.Content;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    Task<ParsedTorrentInfo> ParseTorrentFile(string remoteUrl, CancellationToken cancellationToken);
}

public sealed record ParsedTorrentInfo(string Size, List<TorrentFile> Files);

public sealed record TorrentFile(string FileName, string FilePath, long FileSize);
