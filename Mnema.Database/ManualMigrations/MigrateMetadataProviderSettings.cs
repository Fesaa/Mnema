using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Database.ManualMigrations;

public class MigrateMetadataProviderSettings: ManualMigration
{
    protected override string MigrationName => nameof(MigrateMetadataProviderSettings);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        var oldMetadataProviderSettings =
            await settingsService.GetSettingsAsync<Dictionary<MetadataProvider, MetadataProviderSettingsDto>>(ServerSettingKey.MetadataProviderSettings);

        foreach (var kv in oldMetadataProviderSettings)
        {
            var metadataProvider = kv.Key;
            var settings = kv.Value;

            ctx.MetadataProviderSettings.Add(new MetadataProviderSettings
            {
                MetadataProvider = metadataProvider == MetadataProvider.Upsteam ? MetadataProvider.Upstream : metadataProvider,
                Priority = settings.Priority,
                Enabled = settings.Enabled,
                SeriesTitle = settings.SeriesSettings.Title,
                SeriesSummary = settings.SeriesSettings.Summary,
                SeriesLocalizedName = settings.SeriesSettings.LocalizedSeries,
                SeriesCoverUrl = settings.SeriesSettings.CoverUrl,
                SeriesPublicationStatus = settings.SeriesSettings.PublicationStatus,
                SeriesYear = settings.SeriesSettings.Year,
                SeriesTags = settings.SeriesSettings.Tags,
                SeriesPeople = settings.SeriesSettings.People,
                SeriesLinks = settings.SeriesSettings.Links,
                Chapters = settings.SeriesSettings.Chapters,
                ChapterTitle = settings.SeriesSettings.ChapterSettings.Summary,
                ChapterSummary = settings.SeriesSettings.ChapterSettings.Summary,
                ChapterReleaseDate = settings.SeriesSettings.ChapterSettings.ReleaseDate,
                ChapterPeople = settings.SeriesSettings.ChapterSettings.People,
                ChapterTags = settings.SeriesSettings.ChapterSettings.Tags,
                MetadataProviderSpecific = new MetadataBag(),
            });
        }

        await ctx.SaveChangesAsync();
    }
}
