using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.API;

public interface ISettingsRepository
{
    void Add(ServerSetting setting);
    void Update(ServerSetting setting);
    void Update(Preferences preferences);

    Task<ServerSetting?> GetSettingAsync(string key, CancellationToken ct = default);
    Task<IList<ServerSetting>> GetSettingsAsync(CancellationToken ct = default);
    Task<Preferences> GetPreferencesAsync(CancellationToken ct = default);
}

public interface ISettingsService
{
    Task UpdatePreferences(PreferencesDto dto, CancellationToken cancellationToken);

    Task<T> GetSettingAsync<T>(IMetadataKey<T> key, CancellationToken ct = default);
    Task SetSettingAsync<T>(IMetadataKey<T> key, T value, CancellationToken ct = default);

    Task<ServerSettingsDto> GetSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(UpdateServerSettingsDto settings, CancellationToken ct = default);
}
