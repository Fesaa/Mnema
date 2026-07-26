using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.DTOs;
using Mnema.Models.Entities;

namespace Mnema.Database.Repositories;

public class ConnectionRepository(MnemaDataContext ctx, IMapper mapper)
    : AbstractEntityEntityRepository<Connection, ConnectionDto>(ctx, mapper), IConnectionRepository
{
    public Task<bool> ConnectionExistsForType(ConnectionType type, CancellationToken cancellationToken)
    {
        return ctx.Connections
            .Where(c => c.Type == type)
            .AnyAsync(cancellationToken);
    }
}
