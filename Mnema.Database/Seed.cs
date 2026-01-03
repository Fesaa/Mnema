using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Mnema.Models.Entities;

namespace Mnema.Database;

public static class Seed
{
    private static readonly IList<ServerSetting> DefaultServerSettings = [
        new () { Key = ServerSettingKey.MaxConcurrentTorrents, Value = "5"}, 
        new () { Key = ServerSettingKey.MaxConcurrentImages, Value = "5"}, 
        new () { Key = ServerSettingKey.RootDir, Value = ""}, 
        new () { Key = ServerSettingKey.InstalledVersion, Value = ""}, 
        new () { Key = ServerSettingKey.FirstInstalledVersion, Value = ""}, 
        new () { Key = ServerSettingKey.InstallDate, Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}, 
        new () { Key = ServerSettingKey.SubscriptionRefreshHour, Value = "21"}, 
        new () { Key = ServerSettingKey.LastUpdateDate, Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)}, 
    ];
    
    public static async Task SeedDatabase(this MnemaDataContext ctx)
    {
        foreach (var defaultServerSetting in DefaultServerSettings)
        {
            var existing = await ctx.ServerSettings.FirstOrDefaultAsync(s => s.Key == defaultServerSetting.Key);
            if (existing == null)
            {
                ctx.ServerSettings.Add(defaultServerSetting);
            }
        }


        await ctx.SaveChangesAsync();
    }
}