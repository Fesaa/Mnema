using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.API.Services;

public interface ISettingsService
{
    /// <summary>
    /// You will be required to specify the correct type, there is no compile time checks. Only run time!
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> GetSettingsAsync<T>(ServerSettingKey key);
    Task<ServerSettingsDto> GetSettingsAsync();
    Task SaveSettingsAsync(ServerSettingsDto settings);
}