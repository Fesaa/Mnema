using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mnema.API.Content;

namespace Mnema.Database.ManualMigrations;

public class SetDefaultNamingPreferences: ManualMigration
{
    protected override string MigrationName => nameof(SetDefaultNamingPreferences);
    protected override async Task ExecuteAsync(IServiceProvider serviceProvider, MnemaDataContext ctx, ILogger logger)
    {
        var pref = await ctx.Preferences.SingleAsync();

        if (string.IsNullOrEmpty(pref.ChapterFileFormat))
            pref.ChapterFileFormat = INamingService.DefaultChapterFormat;
        if (string.IsNullOrEmpty(pref.OneShotFileFormat))
            pref.OneShotFileFormat = INamingService.DefaultOneShotFormat;

        await ctx.SaveChangesAsync();
    }
}
