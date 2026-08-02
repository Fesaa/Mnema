using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Models.DTOs;
using Mnema.Models.Entities.Authentication;

namespace Mnema.Database.Repositories;

public class AuthKeyRepository(MnemaDataContext ctx, IMapper mapper) : AbstractEntityEntityRepository<AuthKey, AuthKeyDto>(ctx, mapper), IAuthKeyRepository
{

    public Task<AuthKey?> GetAuthKey(string key, CancellationToken cancellationToken)
    {
        return ctx.AuthKeys
            .Where(k => k.Key == key)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<AuthKey?> GetAuthKeyWithRoles(List<string> roles, CancellationToken cancellationToken)
    {
        return ctx.AuthKeys
            .Where(k => roles.All(r => k.Roles.Contains(r)))
            .OrderByDescending(k => k.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
