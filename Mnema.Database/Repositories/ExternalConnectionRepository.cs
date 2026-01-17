using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Database.Repositories;

public class ConnectionRepository(MnemaDataContext ctx, IMapper mapper) : IConnectionRepository
{
    public Task<List<Connection>> GetAllConnections(CancellationToken cancellationToken)
    {
        return ctx.Connections
            .ToListAsync(cancellationToken);
    }

    public Task<PagedList<ExternalConnectionDto>> GetAllConnectionDtos(PaginationParams pagintation,
        CancellationToken cancellationToken)
    {
        return ctx.Connections
            .ProjectTo<ExternalConnectionDto>(mapper.ConfigurationProvider)
            .OrderBy(c => c.Name)
            .AsPagedList(pagintation, cancellationToken);
    }

    public Task<Connection?> GetConnectionById(Guid connectionId, CancellationToken cancellationToken)
    {
        return ctx.Connections
            .Where(c => c.Id == connectionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task DeleteConnectionById(Guid connectionId, CancellationToken cancellationToken)
    {
        return ctx.Connections
            .Where(c => c.Id == connectionId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public void Add(Connection connection)
    {
        ctx.Connections.Add(connection).State = EntityState.Added;
    }

    public void Update(Connection connection)
    {
        ctx.Connections.Update(connection).State = EntityState.Modified;
    }
}