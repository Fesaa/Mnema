using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API.External;
using Mnema.Models.DTOs;
using Mnema.Models.Entities.External;

namespace Mnema.Database.Repositories;

public class ExternalConnectionRepository(MnemaDataContext ctx, IMapper mapper): IExternalConnectionRepository
{
    public Task<List<ExternalConnection>> GetAllConnections(CancellationToken cancellationToken)
    {
        return ctx.ExternalConnections
            .ToListAsync(cancellationToken);
    }

    public Task<List<ExternalConnectionDto>> GetAllConnectionDtos(CancellationToken cancellationToken)
    {
        return ctx.ExternalConnections
            .ProjectTo<ExternalConnectionDto>(mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }

    public void Add(ExternalConnection connection)
    {
        ctx.ExternalConnections.Add(connection).State = EntityState.Added;
    }

    public void Update(ExternalConnection connection)
    {
        ctx.ExternalConnections.Update(connection).State = EntityState.Modified;
    }
}