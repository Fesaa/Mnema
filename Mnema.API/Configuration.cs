using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.UI;

namespace Mnema.API;

public interface IConfigurationProvider
{
    Task<List<FormControlDefinition>> GetFormControls(CancellationToken cancellationToken);
    Task ReloadConfiguration(CancellationToken cancellationToken);
}
