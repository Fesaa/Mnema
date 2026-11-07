using System;
using Mnema.Common;

namespace Mnema.API;

public static class ServerSettingKeys
{
    public static readonly IMetadataKey<string> InstalledVersion =
        MetadataKeys.String("installedVersion");

    public static readonly IMetadataKey<string> FirstInstalledVersion =
        MetadataKeys.String("firstInstalledVersion");

    public static readonly IMetadataKey<DateTime> InstallDate =
        MetadataKeys.DateTime("installDate", DateTime.MinValue);

    public static readonly IMetadataKey<DateTime> LastUpdateDate =
        MetadataKeys.DateTime("lastUpdateDate", DateTime.MinValue);

    public static readonly IMetadataKey<int> MaxConcurrentImages =
        MetadataKeys.Int("maxConcurrentImages", 5);

    public static readonly IMetadataKey<int> AutoDisableProviderAfter =
        MetadataKeys.Int("autoDisableProviderAfter", 5);

    public static readonly IMetadataKey<bool> ImageConversionLossless =
        MetadataKeys.Bool("imageConversionLossless");

    public static readonly IMetadataKey<int> ImageConversionQuality =
        MetadataKeys.Int("imageConversionQuality", 80);

    public static readonly IMetadataKey<string> Password =
        MetadataKeys.String("password");
}
