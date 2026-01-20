using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Database;

public static class Seed
{
    private static readonly IList<ServerSetting> DefaultServerSettings =
    [
        new() { Key = ServerSettingKey.MaxConcurrentTorrents, Value = "5" },
        new() { Key = ServerSettingKey.MaxConcurrentImages, Value = "5" },
        new() { Key = ServerSettingKey.InstalledVersion, Value = "" },
        new() { Key = ServerSettingKey.FirstInstalledVersion, Value = "" },
        new() { Key = ServerSettingKey.InstallDate, Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
        new() { Key = ServerSettingKey.SubscriptionRefreshHour, Value = "21" },
        new() { Key = ServerSettingKey.LastUpdateDate, Value = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) },
        new() { Key = ServerSettingKey.MetadataProviderSettings, Value = JsonSerializer.Serialize(new Dictionary<MetadataProvider, MetadataProviderSettingsDto>())}
    ];

    public static async Task SeedDatabase(this MnemaDataContext ctx)
    {
        foreach (var defaultServerSetting in DefaultServerSettings)
        {
            var existing = await ctx.ServerSettings.FirstOrDefaultAsync(s => s.Key == defaultServerSetting.Key);
            if (existing == null) ctx.ServerSettings.Add(defaultServerSetting);
        }


        await ctx.SaveChangesAsync();
    }
}
