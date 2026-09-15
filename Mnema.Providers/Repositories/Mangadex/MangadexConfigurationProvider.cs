using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.UI;

namespace Mnema.Providers.Mangadex;

public class MangadexConfigurationProvider: IConfigurationProvider
{
    internal static readonly IMetadataKey<IEnumerable<string>> BlockedGroups = MetadataKeys.Strings(nameof(BlockedGroups));

    public Task<List<FormFieldDefinition>> GetFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormFieldDefinition>
        {
            new CommaSeparatedValuesFieldDefinition
            {
                Key = nameof(BlockedGroups).ToCamelCase(),
                ForceSingle = true,
            }
        });
    }

    public Task ReloadConfiguration(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
