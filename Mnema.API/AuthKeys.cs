using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.Authentication;

namespace Mnema.API;

public interface IAuthKeyRepository : IEntityRepository<AuthKey, AuthKeyDto>
{
    public Task<AuthKey?> GetAuthKey(string key, CancellationToken cancellationToken);
    public Task<AuthKey?> GetAuthKeyWithRoles(List<string> roles, CancellationToken cancellationToken);
}

public interface IAuthKeyService
{
    Task CreateAuthKey(AuthKeyDto dto, CancellationToken cancellationToken);
    Task UpdateAuthKey(AuthKeyDto dto, CancellationToken cancellationToken);
    List<FormControlDefinition> GetAuthKeyForm(ClaimsPrincipal principal);
}
