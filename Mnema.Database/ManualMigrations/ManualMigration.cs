using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.Models.Entities;

namespace Mnema.Database.ManualMigrations;

public abstract class ManualMigration
{
    protected abstract string MigrationName { get; }

    /// <summary>
    /// Execute the migration logic. Handle your own exceptions.
    /// </summary>
    protected abstract Task ExecuteAsync(MnemaDataContext ctx, ILogger logger);


    public async Task RunAsync(MnemaDataContext ctx, ILogger logger)
    {
        ctx.ChangeTracker.Clear();

        if (await ctx.ManualMigrationHistory.AnyAsync(m => m.Name == MigrationName))
        {
            return;
        }
        logger.LogCritical("Running {MigrationName} migration - Please be patient, this may take some time. This is not an error", MigrationName);

        await ExecuteAsync(ctx, logger);

        await ctx.ManualMigrationHistory.AddAsync(new ManualMigrationHistory
        {
            Name = MigrationName
        });
        await ctx.SaveChangesAsync();

        logger.LogCritical("Running {MigrationName} migration - Completed. This is not an error", MigrationName);
    }
}
