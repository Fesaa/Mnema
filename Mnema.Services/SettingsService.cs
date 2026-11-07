using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Services;

internal class SettingsService(IUnitOfWork unitOfWork, INamingService namingService) : ISettingsService
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

    public async Task<T> GetSettingAsync<T>(IMetadataKey<T> key, CancellationToken ct = default)
    {
        var setting = await unitOfWork.SettingsRepository.GetSettingAsync(key.Key, ct);

        var bag = new MetadataBag();
        if (setting != null)
        {
            bag.SetValue(setting.Key, setting.Value);
        }

        return bag.GetKey(key);
    }

    public async Task SetSettingAsync<T>(IMetadataKey<T> key, T value, CancellationToken ct = default)
    {
        var setting = await unitOfWork.SettingsRepository.GetSettingAsync(key.Key, ct);
        var serialized = Serialize(key, value);

        if (setting == null)
        {
            unitOfWork.SettingsRepository.Add(new ServerSetting { Key = key.Key, Value = serialized });
        }
        else if (setting.Value == serialized)
        {
            return;
        }
        else
        {
            setting.Value = serialized;
            unitOfWork.SettingsRepository.Update(setting);
        }

        await unitOfWork.CommitAsync(ct);
    }

    public async Task<ServerSettingsDto> GetSettingsAsync(CancellationToken ct = default)
    {
        var bag = await GetBagAsync(ct);

        return new ServerSettingsDto
        {
            InstalledVersion = bag.GetKey(ServerSettingKeys.InstalledVersion),
            FirstInstalledVersion = bag.GetKey(ServerSettingKeys.FirstInstalledVersion),
            InstallDate = bag.GetKey(ServerSettingKeys.InstallDate),
            LastUpdateDate = bag.GetKey(ServerSettingKeys.LastUpdateDate),
            MaxConcurrentImages = bag.GetKey(ServerSettingKeys.MaxConcurrentImages),
            AutoDisableProviderAfter = bag.GetKey(ServerSettingKeys.AutoDisableProviderAfter),
            ImageConversionLossless = bag.GetKey(ServerSettingKeys.ImageConversionLossless),
            ImageConversionQuality = bag.GetKey(ServerSettingKeys.ImageConversionQuality),
        };
    }

    public async Task SaveSettingsAsync(UpdateServerSettingsDto dto, CancellationToken ct = default)
    {
        var bag = await GetBagAsync(ct);

        bag.SetKey(ServerSettingKeys.MaxConcurrentImages, dto.MaxConcurrentImages);
        bag.SetKey(ServerSettingKeys.AutoDisableProviderAfter, dto.AutoDisableProviderAfter);
        bag.SetKey(ServerSettingKeys.ImageConversionLossless, dto.ImageConversionLossless);
        bag.SetKey(ServerSettingKeys.ImageConversionQuality, dto.ImageConversionQuality);

        await SaveBagAsync(bag, ct);
    }

    private async Task<MetadataBag> GetBagAsync(CancellationToken ct)
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsAsync(ct);
        var bag = new MetadataBag();

        foreach (var setting in settings)
        {
            bag.SetValue(setting.Key, setting.Value);
        }

        return bag;
    }

    private async Task SaveBagAsync(MetadataBag bag, CancellationToken ct)
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsAsync(ct);
        var byKey = settings.ToDictionary(s => s.Key);
        var changed = false;

        foreach (var (key, values) in bag)
        {
            var value = values.Count == 0 ? string.Empty : values[0];

            if (!byKey.TryGetValue(key, out var setting))
            {
                unitOfWork.SettingsRepository.Add(new ServerSetting { Key = key, Value = value });
                changed = true;
                continue;
            }

            if (setting.Value == value) continue;

            setting.Value = value;
            unitOfWork.SettingsRepository.Update(setting);
            changed = true;
        }

        if (changed)
        {
            await unitOfWork.CommitAsync(ct);
        }
    }

    private static string Serialize<T>(IMetadataKey<T> key, T value)
    {
        var bag = new MetadataBag();
        bag.SetKey(key, value);

        return bag.TryGetValue(key.Key, out var values) && values.Count > 0
            ? values[0]
            : string.Empty;
    }
}
