using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Database.ManualMigrations;

public class MigrateMetadataProviderSettings: ManualMigration
{
    private const string LegacyKey = "metadataProviderSettings";

    protected override string MigrationName => nameof(MigrateMetadataProviderSettings);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var row = await ctx.ServerSettings.SingleOrDefaultAsync(s => s.Key == LegacyKey);
        if (row == null) return;

        var oldMetadataProviderSettings =
            JsonSerializer.Deserialize<Dictionary<MetadataProvider, MetadataProviderSettingsDto>>(row.Value) ?? [];
        var existing = await ctx.MetadataProviderSettings.ToListAsync();

        foreach (var kv in oldMetadataProviderSettings)
        {
            var metadataProvider = kv.Key == MetadataProvider.Upsteam ? MetadataProvider.Upstream : kv.Key;
            var settings = kv.Value;

            var target = existing.FirstOrDefault(s => s.MetadataProvider == metadataProvider);
            if (target == null)
            {
                target = new MetadataProviderSettings
                {
                    MetadataProvider = metadataProvider,
                    MetadataProviderSpecific = new MetadataBag(),
                };
                ctx.MetadataProviderSettings.Add(target);
                existing.Add(target);
            }

            target.Priority = settings.Priority;
            target.Enabled = settings.Enabled;
            target.SeriesTitle = settings.SeriesSettings.Title;
            target.SeriesSummary = settings.SeriesSettings.Summary;
            target.SeriesLocalizedName = settings.SeriesSettings.LocalizedSeries;
            target.SeriesCoverUrl = settings.SeriesSettings.CoverUrl;
            target.SeriesPublicationStatus = settings.SeriesSettings.PublicationStatus;
            target.SeriesYear = settings.SeriesSettings.Year;
            target.SeriesTags = settings.SeriesSettings.Tags;
            target.SeriesPeople = settings.SeriesSettings.People;
            target.SeriesLinks = settings.SeriesSettings.Links;
            target.Chapters = settings.SeriesSettings.Chapters;
            target.ChapterTitle = settings.SeriesSettings.ChapterSettings.Summary;
            target.ChapterSummary = settings.SeriesSettings.ChapterSettings.Summary;
            target.ChapterReleaseDate = settings.SeriesSettings.ChapterSettings.ReleaseDate;
            target.ChapterPeople = settings.SeriesSettings.ChapterSettings.People;
            target.ChapterTags = settings.SeriesSettings.ChapterSettings.Tags;
        }

        await ctx.SaveChangesAsync();
    }
}
