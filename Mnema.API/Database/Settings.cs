using Mnema.Models.Entities;

namespace Mnema.API.Database;

public interface ISettingsRepository
{
    void Update(ServerSetting settings);
    void Remove(ServerSetting setting);

    Task<ServerSetting> GetSettingsAsync(ServerSettingKey key);
    Task<IList<ServerSetting>> GetSettingsAsync();
}