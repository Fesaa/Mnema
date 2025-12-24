using System.Globalization;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Services;

internal class SettingsService(ILogger<SettingsService> logger, IUnitOfWork unitOfWork): ISettingsService
{
    public async Task<T> GetSettingsAsync<T>(ServerSettingKey key)
    {
        if (!ServerSettingTypeMap.KeyToType.TryGetValue(key, out var expectedType) || expectedType != typeof(T))
        {
            throw new ArgumentException($"Invalid type {typeof(T).Name} for key {key}. Expected {expectedType?.Name ?? "unknown"}");
        }

        var setting = await unitOfWork.SettingsRepository.GetSettingsAsync(key);
        return DeserializeSetting<T>(setting);
    }

    private static T DeserializeSetting<T>(ServerSetting setting)
    {
        object? result = setting.Key switch
        {
            ServerSettingKey.MaxConcurrentTorrents => int.Parse(setting.Value),
            ServerSettingKey.MaxConcurrentImages => int.Parse(setting.Value),
            ServerSettingKey.RootDir => setting.Value,
            ServerSettingKey.InstalledVersion => setting.Value,
            ServerSettingKey.FirstInstalledVersion => setting.Value,
            ServerSettingKey.InstallDate => DateTime.Parse(setting.Value, CultureInfo.InvariantCulture),
            ServerSettingKey.SubscriptionRefreshHour => int.Parse(setting.Value),
            ServerSettingKey.LastUpdateDate => DateTime.Parse(setting.Value, CultureInfo.InvariantCulture),
            _ => default(T),
        };

        return result switch
        {
            null => throw new ArgumentException($"No converter found for key {setting.Key}"),
            T typedResult => typedResult,
            _ => throw new ArgumentException($"Failed to convert {setting.Key} - {setting.Value} to type {typeof(T).Name}")
        };
    }

    private static async Task<string> SerializeSetting(ServerSettingKey key, object setting)
    {
        return key switch
        {
            ServerSettingKey.MaxConcurrentTorrents => setting.ToString(),
            ServerSettingKey.MaxConcurrentImages => setting.ToString(),
            ServerSettingKey.RootDir => setting.ToString(),
            ServerSettingKey.InstalledVersion => setting.ToString(),
            ServerSettingKey.FirstInstalledVersion => setting.ToString(),
            ServerSettingKey.InstallDate => setting.ToString(),
            ServerSettingKey.SubscriptionRefreshHour => setting.ToString(),
            ServerSettingKey.LastUpdateDate => setting.ToString(),
            _ => throw new ArgumentException($"No converter found for key {key}"),
        } ?? string.Empty;
    }

    public async Task<ServerSettingsDto> GetSettingsAsync()
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsAsync();
        var dto = new ServerSettingsDto();

        foreach (var serverSetting in settings)
        {
            switch (serverSetting.Key)
            {
                case ServerSettingKey.MaxConcurrentTorrents:
                    dto.MaxConcurrentTorrents = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.MaxConcurrentImages:
                    dto.MaxConcurrentImages = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.RootDir:
                    dto.RootDir = DeserializeSetting<string>(serverSetting);
                    break;
                case ServerSettingKey.InstalledVersion:
                    dto.InstalledVersion = DeserializeSetting<string>(serverSetting);
                    break;
                case ServerSettingKey.FirstInstalledVersion:
                    dto.FirstInstalledVersion = DeserializeSetting<string>(serverSetting);
                    break;
                case ServerSettingKey.InstallDate:
                    dto.InstallDate = DeserializeSetting<DateTime>(serverSetting);
                    break;
                case ServerSettingKey.SubscriptionRefreshHour:
                    dto.SubscriptionRefreshHour = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.LastUpdateDate:
                    dto.InstallDate = DeserializeSetting<DateTime>(serverSetting);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverSetting.Key), serverSetting.Key, "Unknown server settings key");
            }
        }

        return dto;
    }

    private async Task<bool> UpdateIfDifferent(ServerSetting setting, object value)
    {
        var serialized = await SerializeSetting(setting.Key, value);
        if (setting.Value != serialized)
        {
            setting.Value = serialized;
            unitOfWork.SettingsRepository.Update(setting);
            return true;
        }

        return false;
    }

    public async Task SaveSettingsAsync(UpdateServerSettingsDto dto)
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsAsync();

        foreach (var serverSetting in settings)
        {
            object? value = serverSetting.Key switch
            {
                ServerSettingKey.MaxConcurrentTorrents => dto.MaxConcurrentTorrents,
                ServerSettingKey.MaxConcurrentImages => dto.MaxConcurrentImages,
                ServerSettingKey.RootDir => dto.RootDir,
                ServerSettingKey.InstalledVersion => null,
                ServerSettingKey.FirstInstalledVersion => null,
                ServerSettingKey.InstallDate => null,
                ServerSettingKey.SubscriptionRefreshHour => dto.SubscriptionRefreshHour,
                ServerSettingKey.LastUpdateDate => null,
                _ => throw new ArgumentOutOfRangeException(nameof(serverSetting.Key), serverSetting.Key, "Unknown server settings key"),
            };

            if (value == null) continue;

            var updated = await UpdateIfDifferent(serverSetting, value);
        }

        if (unitOfWork.HasChanges())
        {
            await unitOfWork.CommitAsync();
        }
    }

    private static class ServerSettingTypeMap
    {
        public static readonly Dictionary<ServerSettingKey, Type> KeyToType = new ()
        {
            { ServerSettingKey.MaxConcurrentTorrents, typeof(int)}, 
            { ServerSettingKey.MaxConcurrentImages, typeof(int)}, 
            { ServerSettingKey.RootDir, typeof(string)}, 
            { ServerSettingKey.InstalledVersion, typeof(string)}, 
            { ServerSettingKey.FirstInstalledVersion, typeof(string)}, 
            { ServerSettingKey.InstallDate, typeof(DateTime)}, 
            { ServerSettingKey.SubscriptionRefreshHour, typeof(int)}, 
            { ServerSettingKey.LastUpdateDate, typeof(DateTime)}, 
        };
    }
}
