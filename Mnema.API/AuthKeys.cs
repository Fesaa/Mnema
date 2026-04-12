using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Common;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.User;

namespace Mnema.API;

public interface IAuthKeyRepository : IEntityRepository<AuthKey, AuthKeyDto>
{
    public Task<PagedList<AuthKeyDto>> GetAuthKeysByUser(Guid userId, PaginationParams paginationParams, CancellationToken cancellationToken);
    public Task<AuthKey?> GetAuthKey(string key, CancellationToken cancellationToken);
}

public interface IAuthKeyService
{
    Task CreateAuthKey(Guid userId, AuthKeyDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task UpdateAuthKey(Guid id, AuthKeyDto dto, ClaimsPrincipal principal, CancellationToken cancellationToken);
    List<FormControlDefinition> GetAuthKeyForm(ClaimsPrincipal principal);
}
