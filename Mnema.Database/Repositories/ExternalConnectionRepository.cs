using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Database.Extensions;
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

    public Task<PagedList<ExternalConnectionDto>> GetAllConnectionDtos(PaginationParams pagintation, CancellationToken cancellationToken)
    {
        return ctx.ExternalConnections
            .ProjectTo<ExternalConnectionDto>(mapper.ConfigurationProvider)
            .OrderBy(c => c.Name)
            .AsPagedList(pagintation, cancellationToken);
    }

    public Task<ExternalConnection?> GetConnectionById(Guid connectionId, CancellationToken cancellationToken)
    {
        return ctx.ExternalConnections
            .Where(c => c.Id == connectionId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task DeleteConnectionById(Guid connectionId, CancellationToken cancellationToken)
    {
        return ctx.ExternalConnections
            .Where(c => c.Id == connectionId)
            .ExecuteDeleteAsync(cancellationToken);
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