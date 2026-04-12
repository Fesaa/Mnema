using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Mnema.API;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;

namespace Mnema.Database.Repositories;

public class AuthKeyRepository(MnemaDataContext ctx, IMapper mapper) : AbstractEntityEntityRepository<AuthKey, AuthKeyDto>(ctx, mapper), IAuthKeyRepository
{
    public Task<PagedList<AuthKeyDto>> GetAuthKeysByUser(Guid userId, PaginationParams paginationParams, CancellationToken cancellationToken)
    {
        return ctx.AuthKeys
            .Where(k => k.UserId == userId)
            .ProjectTo<AuthKeyDto>(mapper.ConfigurationProvider)
            .OrderByDescending(k => k.CreatedUtc)
            .AsPagedList(paginationParams, cancellationToken);
    }

    public Task<AuthKey?> GetAuthKey(string key, CancellationToken cancellationToken)
    {
        return ctx.AuthKeys
            .Where(k => k.Key == key)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
