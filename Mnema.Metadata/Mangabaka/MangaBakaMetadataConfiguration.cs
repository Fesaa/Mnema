using Mnema.API;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models;
using Mnema.Models.DTOs.UI;

namespace Mnema.Metadata.Mangabaka;


internal class MangaBakaMetadataConfiguration: IConfigurationProvider
{
    internal static readonly IMetadataKey<IEnumerable<string>> SeriesNameLanguagePriority = MetadataKeys.Strings(nameof(SeriesNameLanguagePriority));
    internal static readonly IMetadataKey<IEnumerable<string>> LocalizedSeriesNameLanguagePriority = MetadataKeys.Strings(nameof(LocalizedSeriesNameLanguagePriority));
    internal static readonly IMetadataKey<MangabakaTagWeight> MinimumTagWeight = MetadataKeys.Enum<MangabakaTagWeight>(nameof(MinimumTagWeight), MangabakaTagWeight.Recurrent);

    public Task<List<FormFieldDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormFieldDefinition>
        {
            new CommaSeparatedValuesFieldDefinition
            {
                Key = SeriesNameLanguagePriority.Key,
                ForceSingle = true,
            },
            new CommaSeparatedValuesFieldDefinition
            {
                Key = LocalizedSeriesNameLanguagePriority.Key,
                ForceSingle = true,
            },
            FormFieldDefinitions.EnumMetadataDropDown(MinimumTagWeight, "minimum-tag-weight-pipe") with
            {
                DefaultValue = MangabakaTagWeight.Recurrent
            }
        });
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken) => Task.CompletedTask;
}
