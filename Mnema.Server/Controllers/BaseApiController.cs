using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BaseApiController: ControllerBase
{

    private Lazy<Guid> LazyUserId => new (() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException()));

    protected Guid UserId => LazyUserId.Value;
    protected string UserName => User.FindFirst(IOpenIdConnectService.PreferredUsername)?.Value ?? User.FindFirst(ClaimTypes.GivenName)?.Value ?? throw new UnauthorizedAccessException();
    protected IEnumerable<string> UserRoles => User.FindAll(ClaimTypes.Role).Where(c => Roles.AllRoles.Contains(c.Value)).Select(r => r.Value);


}