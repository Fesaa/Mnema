using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.Entities;

namespace Mnema.Database.Repositories;

public class SettingsRepository(MnemaDataContext ctx, IMapper mapper) : ISettingsRepository
{
    public void Update(ServerSetting settings)
    {
        ctx.Entry(settings).State = EntityState.Modified;
    }

    public void Remove(ServerSetting setting)
    {
        ctx.Remove(setting);
    }

    public void Update(Preferences preferences)
    {
        ctx.Entry(preferences).State = EntityState.Modified;
    }

    public async Task<ServerSetting> GetSettingsAsync(ServerSettingKey key)
    {
        return await ctx.ServerSettings
            .Where(x => x.Key == key)
            .FirstAsync();
    }

    public async Task<IList<ServerSetting>> GetSettingsAsync()
    {
        return await ctx.ServerSettings.ToListAsync();
    }

    public Task<Preferences> GetPreferencesAsync(CancellationToken ct = default)
    {
        return ctx.Preferences.FirstAsync(cancellationToken: ct);
    }
}
