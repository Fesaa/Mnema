using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
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
        var authKey = ExtractAuthKey(Request);
        if (string.IsNullOrEmpty(authKey))
        {
            return AuthenticateResult.NoResult();
        }

        var key = await unitOfWork.AuthKeyRepository.GetAuthKey(authKey, Request.HttpContext.RequestAborted);
        if (key == null)
        {
            return AuthenticateResult.Fail(new BadRequestException("Invalid auth key"));
        }

        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaims(Roles.AllRoles
            .Where(r => key.Roles.Contains(r))
            .Select(r => new Claim(ClaimTypes.Role, r)));

        var principal = new ClaimsPrincipal();

        principal.AddIdentity(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private static string? ExtractAuthKey(HttpRequest request)
    {
        if (request.Query.TryGetValue(AuthKeyAuthenticationSchemeOptions.AuthKeyQueryKey, out var values))
        {
            return values.FirstOrDefault();
        }

        if (request.Headers.TryGetValue(AuthKeyAuthenticationSchemeOptions.AuthKeyQueryKey, out var headerValues))
        {
            return headerValues.FirstOrDefault();
        }

        return null;
    }
}
