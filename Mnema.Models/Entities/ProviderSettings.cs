using Microsoft.EntityFrameworkCore;
using Mnema.Common;
using Mnema.Models.Entities.Content;

namespace Mnema.Models.Entities;

[PrimaryKey(nameof(Provider))]
public class ProviderSettings
{
    public Provider Provider { get; set; }

    public MetadataBag Settings { get; set; } = new();

    public bool IsEnabled => !Settings.GetKey(Disable);

    public static readonly IMetadataKey<bool> Disable = MetadataKeys.Bool("disabled");
    public static readonly IMetadataKey<int> ConsecutiveFailures = MetadataKeys.Int("consecutive_failures");
    public static readonly IMetadataKey<bool> BlockAutomaticDownloads = MetadataKeys.Bool("block_automatic_downloads");

}
