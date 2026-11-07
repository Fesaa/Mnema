using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.Entities;

namespace Mnema.Database.Repositories;

public class SettingsRepository(MnemaDataContext ctx) : ISettingsRepository
{
    public void Add(ServerSetting setting)
    {
        ctx.ServerSettings.Add(setting);
    }

    public void Update(ServerSetting setting)
    {
        ctx.Entry(setting).State = EntityState.Modified;
    }

    public void Update(Preferences preferences)
    {
        ctx.Entry(preferences).State = EntityState.Modified;
    }

    public Task<ServerSetting?> GetSettingAsync(string key, CancellationToken ct = default)
    {
        return ctx.ServerSettings.SingleOrDefaultAsync(x => x.Key == key, ct);
    }

    public async Task<IList<ServerSetting>> GetSettingsAsync(CancellationToken ct = default)
    {
        return await ctx.ServerSettings.ToListAsync(ct);
    }

    public Task<Preferences> GetPreferencesAsync(CancellationToken ct = default)
    {
        return ctx.Preferences.SingleAsync(cancellationToken: ct);
    }
}
