using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Models.Entities;
using Mnema.Models.Enums;

namespace Mnema.Database.ManualMigrations;

public class MigrateLinkPreferences: ManualMigration
{
    internal static readonly IMetadataKey<List<LinkFilter>> LinkFilters = MetadataKeys.JsonArray<LinkFilter>(nameof(LinkFilters));

    protected override string MigrationName => nameof(MigrateLinkPreferences);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var mangaBakaPreferences = await ctx.MetadataProviderSettings
            .Where(s => s.MetadataProvider == MetadataProvider.Mangabaka).SingleAsync();
        var preferences = await ctx.Preferences.SingleAsync();

        var links = mangaBakaPreferences.GetKey(LinkFilters);
        if (links.Count == 0) return;

        preferences.LinkFilters = links;
        mangaBakaPreferences.MetadataProviderSpecific.Remove(LinkFilters.Key);
        ctx.MetadataProviderSettings.Update(mangaBakaPreferences);

        await ctx.SaveChangesAsync();

        logger.LogInformation("Migrated {Count} link filters to global preferences", links.Count);
    }
}
