using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public sealed record ParsedTorrentInfo(string Size, List<TorrentFile> Files);

public sealed record TorrentFile(string FileName, string FilePath, long FileSize);

public interface IScannerService
{
    List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    Task<ParsedTorrentInfo> ParseTorrentFile(string remoteUrl, CancellationToken cancellationToken);
}
