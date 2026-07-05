using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.API;

public interface IProviderSettingsRepository
{
    Task<ProviderSettings> GetSettingsForProvider(Provider provider, CancellationToken ct);
    Task<List<ProviderSettings>> GetAllSettings(CancellationToken ct);

    void Update(ProviderSettings settings);
    void Add(ProviderSettings settings);
    void Remove(ProviderSettings settings);
}

public interface IProviderSettingsService
{
    Task UpdateSettings(Provider provider, MetadataBag settings, CancellationToken ct);
    Task<FormDefinition> GetSettingsForm(Provider provider, CancellationToken ct);
}
