using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.Internal;

namespace Mnema.Server.Middleware;

public class AuthKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public const string SchemeName = nameof(AuthKeyAuthenticationSchemeOptions);
    public const string AuthKeyQueryKey = "authKey";
}

public class AuthKeyAuthenticationHandler(
    IOptionsMonitor<AuthKeyAuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IUnitOfWork unitOfWork)
    : AuthenticationHandler<AuthKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Query.TryGetValue(AuthKeyAuthenticationSchemeOptions.AuthKeyQueryKey, out var value))
        {
            return AuthenticateResult.NoResult();
        }

        var authKey = value.FirstOrDefault();
        if (string.IsNullOrEmpty(authKey))
        {
            return AuthenticateResult.NoResult();
        }

        var key = await unitOfWork.AuthKeyRepository.GetAuthKey(authKey, Request.HttpContext.RequestAborted);
        if (key == null)
        {
            return AuthenticateResult.Fail(new MnemaException("Invalid auth key"));
        }

        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaims(Roles.AllRoles
            .Where(r => key.Roles.Contains(r))
            .Select(r => new Claim(ClaimTypes.Role, r)));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, key.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.GivenName, "AuthKey Authenticated"));

        var principal = new ClaimsPrincipal();

        principal.AddIdentity(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
