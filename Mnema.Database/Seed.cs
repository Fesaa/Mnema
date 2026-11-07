using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Common.Extensions;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;
using Mnema.Models.Enums;

namespace Mnema.Database;

public static class Seed
{
    private static readonly IList<ServerSetting> DefaultServerSettings =
    [
        new() { Key = ServerSettingKeys.InstalledVersion.Key, Value = "" },
        new() { Key = ServerSettingKeys.FirstInstalledVersion.Key, Value = "" },
        new() { Key = ServerSettingKeys.InstallDate.Key, Value = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
        new() { Key = ServerSettingKeys.LastUpdateDate.Key, Value = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
    ];

    public static async Task SeedDatabase(this MnemaDataContext ctx)
    {
        foreach (var defaultServerSetting in DefaultServerSettings)
        {
            var existing = await ctx.ServerSettings.FirstOrDefaultAsync(s => s.Key == defaultServerSetting.Key);
            if (existing == null) ctx.ServerSettings.Add(defaultServerSetting);
        }

        await ctx.SaveChangesAsync();

        await SeedMetadataProviderSettings(ctx);
        await SeedProviderSettings(ctx);
        await RemoveDeprecatedProviders(ctx);
        await SeedPreferences(ctx);
    }

    private static async Task SeedMetadataProviderSettings(MnemaDataContext ctx)
    {
        var providers = Enum.GetValues<MetadataProvider>().Where(p => p != MetadataProvider.Upsteam);
        var existing = await ctx.MetadataProviderSettings.Select(s => s.MetadataProvider).ToListAsync();
        var maxPriority = await ctx.MetadataProviderSettings
            .OrderByDescending(s => s.Priority)
            .Select(s => s.Priority)
            .FirstOrDefaultAsync();

        foreach (var metadataProvider in providers.Where(p => !existing.Contains(p)))
        {
            ctx.MetadataProviderSettings.Add(new MetadataProviderSettings
            {
                MetadataProvider = metadataProvider,
                Priority = maxPriority++,
                Enabled = true,
                SeriesTitle = true,
                SeriesSummary = true,
                SeriesLocalizedName = true,
                SeriesCoverUrl = true,
                SeriesPublicationStatus = true,
                SeriesYear = true,
                SeriesTags = true,
                SeriesPeople = true,
                SeriesLinks = true,
                Chapters = true,
                ChapterTitle = true,
                ChapterSummary = true,
                ChapterReleaseDate = true,
                ChapterPeople = true,
                ChapterTags = true,
                MetadataProviderSpecific = new MetadataBag()
            });
        }

        await ctx.SaveChangesAsync();
    }

    private static async Task SeedProviderSettings(MnemaDataContext ctx)
    {
        var providers = Enum.GetValues<Provider>();
        var existing = await ctx.ProviderSettings.ToListAsync();
        var missing = providers.Except(existing.Select(p => p.Provider));

        foreach (var provider in missing)
        {
            ctx.ProviderSettings.Add(new ProviderSettings
            {
                Provider = provider,
                Settings = new MetadataBag()
            });
        }

        await ctx.SaveChangesAsync();
    }

    private static async Task RemoveDeprecatedProviders(MnemaDataContext ctx)
    {
        var deprecatedProviders = typeof(Provider).GetFields()
            .Where(f => f.GetCustomAttribute<ObsoleteAttribute>() != null)
            .Select(f => (Provider?)f.GetValue(null))
            .WhereNotNull();

        await ctx.Pages
            .Where(p => deprecatedProviders.Contains(p.Provider))
            .ExecuteDeleteAsync();
    }

    private static async Task SeedPreferences(MnemaDataContext ctx)
    {
        var hasPreferences = await ctx.Preferences.AnyAsync();
        if (hasPreferences) return;

        ctx.Preferences.Add(new Preferences
        {
            ImageFormat = ImageFormat.Upstream,
            CoverFallbackMethod = CoverFallbackMethod.First,
            ConvertToGenreList = [],
            BlackListedTags = [],
            WhiteListedTags = [],
            AgeRatingMappings = [],
            MetadataFieldMappings = [],
            TagMappings = [],
            PinSubscriptionTitles = true,
            ChapterFileFormat = INamingService.DefaultChapterFormat,
            OneShotFileFormat = INamingService.DefaultOneShotFormat,
            LinkFilters = [],
        });

        await ctx.SaveChangesAsync();
    }
}
