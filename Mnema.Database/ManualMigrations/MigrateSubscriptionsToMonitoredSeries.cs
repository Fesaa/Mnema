using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.ManualMigrations;

public class MigrateSubscriptionsToMonitoredSeries: ManualMigration
{
    protected override string MigrationName { get; } = nameof(MigrateSubscriptionsToMonitoredSeries);
    protected override async Task ExecuteAsync(MnemaDataContext ctx, ILogger logger)
    {
        var subs = await ctx.Subscriptions.ToListAsync();

        foreach (var subscription in subs)
        {
            ctx.MonitoredSeries.Add(new MonitoredSeries
            {
                Title = subscription.Title,
                BaseDir = subscription.BaseDir,
                Metadata = subscription.Metadata,
                ExternalId = subscription.ContentId,
                Provider = subscription.Provider,
                TitleOverride = subscription.Metadata.GetKey(RequestConstants.TitleOverride) ?? string.Empty,
                UserId = subscription.UserId,
                ContentFormat = subscription.Metadata.GetKey(RequestConstants.ContentFormatKey),
                Format = subscription.Metadata.GetKey(RequestConstants.FormatKey),
                Chapters = [],
                CoverUrl = string.Empty,
                HardcoverId = string.Empty,
                MangaBakaId = string.Empty,
                RefUrl = string.Empty,
                Summary = string.Empty,
                ValidTitles = []
            });
        }

        await ctx.SaveChangesAsync();
    }
}
