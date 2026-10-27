namespace Mnema.Providers.Managers.QBit;

internal enum TorrentListFilter
{
    All,
    Downloading,
    Completed,
    Paused,
    Active,
    Inactive,
    Seeding,
    Resumed,
    Errored,
    Stalled,
    StalledUploading,
    StalledDownloading,
}
