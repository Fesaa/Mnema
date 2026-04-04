using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mnema.API;
using Mnema.Models.Internal;

namespace Mnema.Server.Middleware;

public class NoAuthAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = nameof(NoAuthAuthenticationSchemeOptions);
}

public class NoAuthAuthenticationHandler(
    IOptionsMonitor<NoAuthAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUnitOfWork unitOfWork)
    : AuthenticationHandler<NoAuthAuthenticationSchemeOptions>(options, logger, encoder)
{

    private static readonly Guid NoAuthUserGuid = Guid.Parse("3f461b21-85f0-4e1e-b64b-fb48c774cdb6");

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Ensure user is created
        await unitOfWork.UserRepository.GetUserById(NoAuthUserGuid);

        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaims(Roles.AllRoles.Select(r => new Claim(ClaimTypes.Role, r)));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, NoAuthUserGuid.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "User"));

        var principal = new ClaimsPrincipal();

        principal.AddIdentity(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
