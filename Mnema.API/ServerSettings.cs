using System.Collections.Generic;
using System.Threading.Tasks;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.API;

public interface ISettingsRepository
{
    void Update(ServerSetting settings);
    void Remove(ServerSetting setting);

    Task<ServerSetting> GetSettingsAsync(ServerSettingKey key);
    Task<IList<ServerSetting>> GetSettingsAsync();
}

public interface ISettingsService
{
    /// <summary>
    ///     You will be required to specify the correct type, there is no compile time checks. Only run time!
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> GetSettingsAsync<T>(ServerSettingKey key);

    Task<ServerSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(UpdateServerSettingsDto settings);
}