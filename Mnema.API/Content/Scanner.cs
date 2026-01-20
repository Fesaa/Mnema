using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.Entities.Content;
using Mnema.Models.Publication;

namespace Mnema.API.Content;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    OnDiskContent ParseContent(string file, ContentFormat contentFormat);

    Task<List<Chapter>> ParseTorrentFile(string remoteUrl, ContentFormat contentFormat, CancellationToken cancellationToken);

    OnDiskContent? FindMatch(List<OnDiskContent> onDiskContents, Chapter chapter);
}
