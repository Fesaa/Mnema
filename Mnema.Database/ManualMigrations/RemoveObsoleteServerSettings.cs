using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mnema.Database.ManualMigrations;

public class RemoveObsoleteServerSettings : ManualMigration
{
    protected override string MigrationName => nameof(RemoveObsoleteServerSettings);

    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        await ctx.ServerSettings
            .Where(s => s.Key == "metadataProviderSettings"
                        || s.Key == "maxConcurrentTorrents"
                        || s.Key == "subscriptionRefreshHour")
            .ExecuteDeleteAsync();
    }
}
