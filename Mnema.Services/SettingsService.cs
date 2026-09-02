using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Services;

internal class SettingsService(ILogger<SettingsService> logger, IUnitOfWork unitOfWork, INamingService namingService) : ISettingsService
{
    public async Task UpdatePreferences(PreferencesDto dto, CancellationToken cancellationToken)
    {
        var pref = await unitOfWork.SettingsRepository.GetPreferencesAsync(cancellationToken);

        pref.ImageFormat = dto.ImageFormat;
        pref.CoverFallbackMethod = dto.CoverFallbackMethod;
        pref.BlackListedTags = NormalizeTags(dto.BlackListedTags);
        pref.WhiteListedTags = NormalizeTags(dto.WhiteListedTags);
        pref.PinSubscriptionTitles = dto.PinSubscriptionTitles;
        pref.LinkFilters = dto.LinkFilters;

        pref.AgeRatingMappings.Clear();
        foreach (var mapping in dto.AgeRatingMappings.DistinctBy(arm => arm.Tag.ToNormalized()))
        {
            pref.AgeRatingMappings.Add(mapping);
        }

        pref.MetadataFieldMappings.Clear();
        foreach (var mapping in dto.MetadataFieldMappings)
        {
            pref.MetadataFieldMappings.Add(mapping);
        }

        if (pref.ChapterFileFormat != dto.ChapterFileFormat)
        {
            if (!namingService.ChapterFormatter.IsValid(dto.ChapterFileFormat))
                throw new BadRequestException("Invalid chapter file format");

            pref.ChapterFileFormat = dto.ChapterFileFormat;
        }

        if (pref.OneShotFileFormat != dto.OneShotFileFormat)
        {
            if (!namingService.OneShotFormatter.IsValid(dto.OneShotFileFormat))
                throw new BadRequestException("Invalid one shot file format");

            pref.OneShotFileFormat = dto.OneShotFileFormat;
        }

        await unitOfWork.CommitAsync(cancellationToken);
        return;

        List<string> NormalizeTags(IEnumerable<string>? tags)
        {
            return (tags ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .DistinctBy(d => d.ToNormalized())
                .Order()
                .ToList();
        }
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
                case ServerSettingKey.LastUpdateDate:
                    dto.InstallDate = DeserializeSetting<DateTime>(serverSetting);
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
                case ServerSettingKey.MaxConcurrentTorrents:
                case ServerSettingKey.SubscriptionRefreshHour:
                case ServerSettingKey.MetadataProviderSettings:
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
                ServerSettingKey.MaxConcurrentTorrents => null,
                ServerSettingKey.MaxConcurrentImages => dto.MaxConcurrentImages,
                ServerSettingKey.InstalledVersion => null,
                ServerSettingKey.FirstInstalledVersion => null,
                ServerSettingKey.InstallDate => null,
                ServerSettingKey.SubscriptionRefreshHour => null,
                ServerSettingKey.LastUpdateDate => null,
                ServerSettingKey.MetadataProviderSettings => null,
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
