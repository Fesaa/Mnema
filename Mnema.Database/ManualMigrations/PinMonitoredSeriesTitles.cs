using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Publication;

namespace Mnema.Database.ManualMigrations;

public class PinMonitoredSeriesTitles: ManualMigration
{
    protected override string MigrationName => nameof(PinMonitoredSeriesTitles);

    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var metadataResolver = serviceProvider.GetRequiredService<IMetadataResolver>();

        var preferences = await ctx.UserPreferences.ToDictionaryAsync(p => p.UserId, p => p.PinSubscriptionTitles);
        if (preferences.All(p => !p.Value)) return;

        var monitoredSeries = await ctx.MonitoredSeries.ToListAsync();
        foreach (var series in monitoredSeries
                     .Where(m => preferences.TryGetValue(m.UserId, out var pin) && pin)
                     .Where(m => string.IsNullOrEmpty(m.TitleOverride)))
        {
            var metadata = series.MetadataForDownloadRequest();

            Series? resolvedSeries;
            try
            {
                resolvedSeries = await metadataResolver.ResolveSeriesAsync(series.Provider, metadata);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resolve series for {Title}, cannot set title override", series.Title);
                await Task.Delay(250);
                continue;
            }

            if (resolvedSeries == null || string.IsNullOrEmpty(resolvedSeries.Title))
            {
                logger.LogWarning("Skipping title pin for series {Title} because it could not be resolved", series.Title);
                continue;
            }

            logger.LogDebug("Setting title override for {Title} to {ResolvedTitle}", series.Title, resolvedSeries.Title);
            series.TitleOverride = resolvedSeries.Title;

            await Task.Delay(250);
        }

        await ctx.SaveChangesAsync();
    }
}
