using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mnema.Database.Extensions;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.ManualMigrations;

public class MigrateValidTitles: ManualMigration
{
    private const int BatchSize = 100;

    protected override string MigrationName => nameof(MigrateValidTitles);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        await foreach (var monitoredSeries in ctx.MonitoredSeries.BatchAsync(BatchSize))
        {
            foreach (var series in monitoredSeries)
            {
                series.SearchTitles =
                [
                    .. series.ValidTitles.Select(t => new SearchTitle
                    {
                        Title = t,
                    })
                ];
            }

            await ctx.SaveChangesAsync();
            ctx.ChangeTracker.Clear();
        }
    }
}
