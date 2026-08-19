using System.Collections.Generic;

namespace Mnema.API.External;

public static class HangfireQueue
{
    private const string Default = "default";
    public const string TorrentCleanup = "torrent-cleanup-queue";
    public const string ImportScanQueue = "import-scan-queue";

    public static readonly IReadOnlyList<string> Queues =
    [
        Default,
        TorrentCleanup,
        ImportScanQueue
    ];
}
