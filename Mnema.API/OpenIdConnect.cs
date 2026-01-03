using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Mnema.API;

public interface IOpenIdConnectService
{
    public const string RefreshToken = "refresh_token";
    public const string IdToken = "id_token";
    public const string ExpiresAt = "expires_at";
    public const string CookieName = ".AspNetCore.Cookies";
    public const string PreferredUsername = "preferred_username";
    
    Task RefreshCookieToken(CookieValidatePrincipalContext ctx);

    Task<ClaimsPrincipal> ParseIdToken(string idToken);
}