namespace Mnema.Providers.Managers.QBit;

internal enum TorrentState
{
    Unknown,
    Error,
    PausedUpload,
    PausedDownload,
    QueuedUpload,
    QueuedDownload,
    Uploading,
    StalledUpload,
    CheckingUpload,
    CheckingDownload,
    Downloading,
    StalledDownload,
    FetchingMetadata,
    ForcedFetchingMetadata,
    ForcedUpload,
    ForcedDownload,
    MissingFiles,
    Allocating,
    QueuedForChecking,
    CheckingResumeData,
    Moving,
}
