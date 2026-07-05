using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

public class ProviderSettingsService(ILogger<ProviderSettingsService> logger, IServiceProvider serviceProvider, IUnitOfWork unitOfWork): IProviderSettingsService
{
    public async Task UpdateSettings(Provider provider, MetadataBag settings, CancellationToken ct)
    {
        var providerSettings = await unitOfWork.ProviderSettingsRepository.GetSettingsForProvider(provider, ct);
        var formDefinition = await GetSettingsForm(provider, ct);

        var newSettings = new MetadataBag();
        foreach (var key in formDefinition.Controls.Select(c => c.Key))
        {
            newSettings.Add(key, settings.GetStrings(key).ToList());
        }

        providerSettings.Settings = newSettings;
        unitOfWork.ProviderSettingsRepository.Update(providerSettings);

        await unitOfWork.CommitAsync(ct);
    }

    public async Task<FormDefinition> GetSettingsForm(Provider provider, CancellationToken ct)
    {
        var configurationProvider = serviceProvider.GetKeyedService<IConfigurationProvider>(provider);
        var extraFormControls = await (configurationProvider?.GetFormControls(ct) ?? Task.FromResult(new List<FormControlDefinition>()));

        return new FormDefinition
        {
            Key = "provider-settings",
            DescriptionKey = "description",
            Controls = [
                new FormControlDefinition
                {
                    Key = ProviderSettings.Disable.Key,
                    Type = FormType.Switch,
                    DefaultOption = false,
                },
                ..extraFormControls
            ]
        };
    }
}
