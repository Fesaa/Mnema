using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Mnema.API.External;
using Mnema.Models.Enums;

namespace Mnema.API.Content;

public interface IScannerService
{
    List<OnDiskContent> ScanDirectory(string path, ContentFormat contentFormat, Format format,
        CancellationToken cancellationToken);

    Task<ParsedTorrentInfo> ParseTorrentFile(string remoteUrl, CancellationToken cancellationToken);

    [Queue(HangfireQueue.ImportScanQueue)]
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 60 * 60)]
    Task ScanRoot(string path, CancellationToken cancellationToken);
}

public sealed record ParsedTorrentInfo(string Size, List<TorrentFile> Files);

public sealed record TorrentFile(string FileName, string FilePath, long FileSize);
