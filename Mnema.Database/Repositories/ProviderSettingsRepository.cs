using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Database.Repositories;

public class ProviderSettingsRepository(MnemaDataContext ctx, IMapper mapper): IProviderSettingsRepository
{
    public Task<ProviderSettings> GetSettingsForProvider(Provider provider, CancellationToken ct)
    {
        return ctx.ProviderSettings
            .Where(ps => ps.Provider == provider)
            .FirstAsync(ct);
    }

    public Task<List<ProviderSettings>> GetAllSettings(CancellationToken ct)
    {
        return ctx.ProviderSettings.ToListAsync(ct);
    }

    public void Update(ProviderSettings settings)
    {
        ctx.ProviderSettings.Update(settings).State = EntityState.Modified;
    }

    public void Add(ProviderSettings settings)
    {
        ctx.ProviderSettings.Add(settings).State = EntityState.Added ;
    }

    public void Remove(ProviderSettings settings)
    {
        ctx.ProviderSettings.Remove(settings).State = EntityState.Deleted;
    }
}
