using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.UI;

namespace Mnema.Metadata.Hardcover;

public class HardcoverMetadataConfiguration: IConfigurationProvider
{
    internal static readonly IMetadataKey<bool> OnlyUseSubtitleAsChapterTitle = MetadataKeys.Bool(nameof(OnlyUseSubtitleAsChapterTitle));

    public Task<List<FormFieldDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormFieldDefinition>()
        {
            new SwitchFieldDefinition()
            {
                Key = OnlyUseSubtitleAsChapterTitle.Key,
            }
        });
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken) => Task.CompletedTask;
}
