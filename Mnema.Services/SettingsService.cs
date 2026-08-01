using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;

namespace Mnema.Services;

internal class SettingsService(ILogger<SettingsService> logger, IUnitOfWork unitOfWork) : ISettingsService
{
    public async Task UpdatePreferences(PreferencesDto dto, CancellationToken cancellationToken)
    {
        var pref = await unitOfWork.SettingsRepository.GetPreferencesAsync(cancellationToken);

        pref.ImageFormat = dto.ImageFormat;
        pref.CoverFallbackMethod = dto.CoverFallbackMethod;
        pref.BlackListedTags = dto.BlackListedTags.DistinctBy(t => t.ToNormalized()).ToList();
        pref.WhiteListedTags = dto.WhiteListedTags.DistinctBy(t => t.ToNormalized()).ToList();
        pref.ConvertToGenreList = dto.ConvertToGenreList.DistinctBy(g => g.ToNormalized()).ToList();
        pref.AgeRatingMappings = dto.AgeRatingMappings.DistinctBy(arm => arm.Tag.ToNormalized()).ToList();
        pref.TagMappings = dto.TagMappings
            .DistinctBy(tm => tm.DestinationTag.ToNormalized() + tm.OriginTag.ToNormalized()).ToList();
        pref.PinSubscriptionTitles = dto.PinSubscriptionTitles;

        unitOfWork.SettingsRepository.Update(pref);

        await unitOfWork.CommitAsync(cancellationToken);
    }

    public async Task<T> GetSettingsAsync<T>(ServerSettingKey key)
    {
        if (!ServerSettingTypeMap.KeyToType.TryGetValue(key, out var expectedType) || expectedType != typeof(T))
            throw new ArgumentException(
                $"Invalid type {typeof(T).Name} for key {key}. Expected {expectedType?.Name ?? "unknown"}");

        var setting = await unitOfWork.SettingsRepository.GetSettingsAsync(key);
        return DeserializeSetting<T>(setting);
    }

    public async Task<ServerSettingsDto> GetSettingsAsync()
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsAsync();
        var dto = new ServerSettingsDto();

        foreach (var serverSetting in settings)
            switch (serverSetting.Key)
            {
                case ServerSettingKey.MaxConcurrentTorrents:
                    dto.MaxConcurrentTorrents = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.MaxConcurrentImages:
                    dto.MaxConcurrentImages = DeserializeSetting<int>(serverSetting);
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
                case ServerSettingKey.MetadataProviderSettings:
                    dto.MetadataProviderSettings =
                        DeserializeSetting<Dictionary<MetadataProvider, MetadataProviderSettingsDto>>(serverSetting);
                    break;
                case ServerSettingKey.AutoDisableAfter:
                    dto.AutoDisableProviderAfter = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.ImageConversionLossLess:
                    dto.ImageConversionLossless = DeserializeSetting<bool>(serverSetting);
                    break;
                case ServerSettingKey.ImageConversionQuality:
                    dto.ImageConversionQuality = DeserializeSetting<int>(serverSetting);
                    break;
                case ServerSettingKey.Password:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serverSetting.Key), serverSetting.Key,
                        "Unknown server settings key");
            }

        return dto;
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
                ServerSettingKey.InstalledVersion => null,
                ServerSettingKey.FirstInstalledVersion => null,
                ServerSettingKey.InstallDate => null,
                ServerSettingKey.SubscriptionRefreshHour => dto.SubscriptionRefreshHour,
                ServerSettingKey.LastUpdateDate => null,
                ServerSettingKey.MetadataProviderSettings => dto.MetadataProviderSettings,
                ServerSettingKey.AutoDisableAfter => dto.AutoDisableProviderAfter,
                ServerSettingKey.ImageConversionLossLess => dto.ImageConversionLossless,
                ServerSettingKey.ImageConversionQuality => dto.ImageConversionQuality,
                ServerSettingKey.Password => null,
                _ => throw new ArgumentOutOfRangeException(nameof(serverSetting.Key), serverSetting.Key,
                    "Unknown server settings key")
            };

            if (value == null) continue;

            var updated = await UpdateIfDifferent(serverSetting, value);
        }

        if (unitOfWork.HasChanges()) await unitOfWork.CommitAsync();
    }

    private static T DeserializeSetting<T>(ServerSetting setting)
    {
        object? result = setting.Key switch
        {
            ServerSettingKey.MaxConcurrentTorrents => int.Parse(setting.Value),
            ServerSettingKey.MaxConcurrentImages => int.Parse(setting.Value),
            ServerSettingKey.InstalledVersion => setting.Value,
            ServerSettingKey.FirstInstalledVersion => setting.Value,
            ServerSettingKey.InstallDate => DateTime.Parse(setting.Value, CultureInfo.InvariantCulture),
            ServerSettingKey.SubscriptionRefreshHour => int.Parse(setting.Value),
            ServerSettingKey.LastUpdateDate => DateTime.Parse(setting.Value, CultureInfo.InvariantCulture),
            ServerSettingKey.MetadataProviderSettings => JsonSerializer.Deserialize<T>(setting.Value),
            ServerSettingKey.AutoDisableAfter => int.Parse(setting.Value),
            ServerSettingKey.ImageConversionLossLess => bool.Parse(setting.Value),
            ServerSettingKey.ImageConversionQuality => int.Parse(setting.Value),
            ServerSettingKey.Password => setting.Value,
            _ => default(T)
        };

        return result switch
        {
            null => throw new ArgumentException($"[DeserializeSetting] No converter found for key {setting.Key}"),
            T typedResult => typedResult,
            _ => throw new ArgumentException(
                $"Failed to convert {setting.Key} - {setting.Value} to type {typeof(T).Name}")
        };
    }

    private static async Task<string> SerializeSetting(ServerSettingKey key, object setting)
    {
        return key switch
        {
            ServerSettingKey.MaxConcurrentTorrents => setting.ToString(),
            ServerSettingKey.MaxConcurrentImages => setting.ToString(),
            ServerSettingKey.InstalledVersion => setting.ToString(),
            ServerSettingKey.FirstInstalledVersion => setting.ToString(),
            ServerSettingKey.InstallDate => setting.ToString(),
            ServerSettingKey.SubscriptionRefreshHour => setting.ToString(),
            ServerSettingKey.LastUpdateDate => setting.ToString(),
            ServerSettingKey.MetadataProviderSettings => JsonSerializer.Serialize(setting),
            ServerSettingKey.AutoDisableAfter => setting.ToString(),
            ServerSettingKey.ImageConversionLossLess => setting.ToString(),
            ServerSettingKey.ImageConversionQuality => setting.ToString(),
            ServerSettingKey.Password => setting.ToString(),
            _ => throw new ArgumentException($"[SerializeSetting] No converter found for key {key}")
        } ?? string.Empty;
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

    private static class ServerSettingTypeMap
    {
        public static readonly Dictionary<ServerSettingKey, Type> KeyToType = new()
        {
            { ServerSettingKey.MaxConcurrentTorrents, typeof(int) },
            { ServerSettingKey.MaxConcurrentImages, typeof(int) },
            { ServerSettingKey.InstalledVersion, typeof(string) },
            { ServerSettingKey.FirstInstalledVersion, typeof(string) },
            { ServerSettingKey.InstallDate, typeof(DateTime) },
            { ServerSettingKey.SubscriptionRefreshHour, typeof(int) },
            { ServerSettingKey.LastUpdateDate, typeof(DateTime) },
            { ServerSettingKey.MetadataProviderSettings, typeof(Dictionary<MetadataProvider, MetadataProviderSettingsDto>)},
            { ServerSettingKey.AutoDisableAfter, typeof(int)},
            { ServerSettingKey.ImageConversionLossLess, typeof(bool) },
            { ServerSettingKey.ImageConversionQuality, typeof(int) },
            { ServerSettingKey.Password, typeof(string) },
        };
    }
}
