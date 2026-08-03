using System;

namespace Mnema.Models.Entities;

public enum ServerSettingKey
{
    [Obsolete("Managed by QBit")]
    MaxConcurrentTorrents = 0,
    MaxConcurrentImages = 1,
    InstalledVersion = 3,
    FirstInstalledVersion = 4,
    InstallDate = 5,
    [Obsolete("RSS sync is every 15m")]
    SubscriptionRefreshHour = 6,
    LastUpdateDate = 7,
    [Obsolete("Use the entity")]
    MetadataProviderSettings = 8,
    AutoDisableAfter = 9,
    ImageConversionLossLess = 10,
    ImageConversionQuality = 11,
    Password = 12,
}
