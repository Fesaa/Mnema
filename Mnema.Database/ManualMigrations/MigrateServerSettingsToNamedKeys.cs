using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mnema.Database.ManualMigrations;

public class MigrateServerSettingsToNamedKeys : ManualMigration
{
    protected override string MigrationName => nameof(MigrateServerSettingsToNamedKeys);

    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        await using var transaction = await ctx.Database.BeginTransactionAsync();

        if (ctx.Database.IsNpgsql())
        {
            await ctx.Database.ExecuteSqlRawAsync(PostgresSql);
        }
        else
        {
            await ctx.Database.ExecuteSqlRawAsync(SqliteSql);
        }

        await transaction.CommitAsync();
    }

    private const string PostgresSql = """
        ALTER TABLE "ServerSettings" ALTER COLUMN "Key" TYPE text USING (
            CASE "Key"::text
                WHEN '0' THEN 'maxConcurrentTorrents'
                WHEN '1' THEN 'maxConcurrentImages'
                WHEN '3' THEN 'installedVersion'
                WHEN '4' THEN 'firstInstalledVersion'
                WHEN '5' THEN 'installDate'
                WHEN '6' THEN 'subscriptionRefreshHour'
                WHEN '7' THEN 'lastUpdateDate'
                WHEN '8' THEN 'metadataProviderSettings'
                WHEN '9' THEN 'autoDisableProviderAfter'
                WHEN '10' THEN 'imageConversionLossless'
                WHEN '11' THEN 'imageConversionQuality'
                WHEN '12' THEN 'password'
                ELSE "Key"::text
            END
        );

        DELETE FROM "ServerSettings"
        WHERE "Key" IN ('maxConcurrentTorrents', 'subscriptionRefreshHour');
        """;

    private const string SqliteSql = """
        DROP TABLE IF EXISTS "ServerSettings_new";

        CREATE TABLE "ServerSettings_new" (
            "Key" TEXT NOT NULL CONSTRAINT "PK_ServerSettings" PRIMARY KEY,
            "Value" TEXT NOT NULL
        );

        INSERT INTO "ServerSettings_new" ("Key", "Value")
        SELECT
            CASE CAST("Key" AS TEXT)
                WHEN '0' THEN 'maxConcurrentTorrents'
                WHEN '1' THEN 'maxConcurrentImages'
                WHEN '3' THEN 'installedVersion'
                WHEN '4' THEN 'firstInstalledVersion'
                WHEN '5' THEN 'installDate'
                WHEN '6' THEN 'subscriptionRefreshHour'
                WHEN '7' THEN 'lastUpdateDate'
                WHEN '8' THEN 'metadataProviderSettings'
                WHEN '9' THEN 'autoDisableProviderAfter'
                WHEN '10' THEN 'imageConversionLossless'
                WHEN '11' THEN 'imageConversionQuality'
                WHEN '12' THEN 'password'
                ELSE CAST("Key" AS TEXT)
            END,
            "Value"
        FROM "ServerSettings";

        DROP TABLE "ServerSettings";
        ALTER TABLE "ServerSettings_new" RENAME TO "ServerSettings";

        DELETE FROM "ServerSettings"
        WHERE "Key" IN ('maxConcurrentTorrents', 'subscriptionRefreshHour');
        """;
}
