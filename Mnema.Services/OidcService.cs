using System.Collections.Concurrent;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Flurl.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Mnema.API;
using Mnema.Models.Internal;

namespace Mnema.Services;


internal class OpenIdConnectService(
    ILogger<OpenIdConnectService> logger,
    ConfigurationManager<OpenIdConnectConfiguration> oidcConfigurationManager,
    OpenIdConnectConfig openIdConnectConfig
    ): IOpenIdConnectService
{

    private static readonly ConcurrentDictionary<string, bool> RefreshInProgress = new();
    
    public async Task RefreshCookieToken(CookieValidatePrincipalContext ctx)
    {
        if (ctx.Principal == null) return;

        var key = ctx.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(key)) return;
        
        var refreshToken = ctx.Properties.GetTokenValue(IOpenIdConnectService.RefreshToken);
        if (string.IsNullOrEmpty(refreshToken)) return;
        
        var expiresAt = ctx.Properties.GetTokenValue(IOpenIdConnectService.ExpiresAt);
        if (string.IsNullOrEmpty(expiresAt)) return;
        
        var tokenExpiry = DateTimeOffset.ParseExact(expiresAt, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        if (tokenExpiry >= DateTimeOffset.UtcNow.AddSeconds(30)) return;
        
        // Ensure we're not refreshing twice
        if (!RefreshInProgress.TryAdd(key, true)) return;

        try
        {
            var tokenResponse = await RefreshTokenAsync(refreshToken);
            if (!string.IsNullOrEmpty(tokenResponse.Error))
            {
                logger.LogError("Failed to refresh token : {Error} - {Description}", tokenResponse.Error, tokenResponse.ErrorDescription);
                throw new UnauthorizedAccessException();
            }
            
            var principal = await ParseIdToken(tokenResponse.IdToken);
            ctx.Principal = principal;
            
            var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(double.Parse(tokenResponse.ExpiresIn));
            ctx.Properties.UpdateTokenValue(IOpenIdConnectService.ExpiresAt, newExpiresAt.ToString("o"));
            ctx.Properties.UpdateTokenValue(IOpenIdConnectService.RefreshToken, tokenResponse.RefreshToken);
            ctx.Properties.UpdateTokenValue(IOpenIdConnectService.IdToken, tokenResponse.IdToken);
            ctx.ShouldRenew = true;
            
            logger.LogDebug("Automatically refreshed token for user {UserId}", key);
        }
        finally
        {
            RefreshInProgress.TryRemove(key, out _);
        }
    }
    
    public async Task<ClaimsPrincipal> ParseIdToken(string idToken)
    {
        var discoveryDocument = await oidcConfigurationManager.GetConfigurationAsync();

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = discoveryDocument.Issuer,
            ValidAudience = openIdConnectConfig.ClientId,
            IssuerSigningKeys = discoveryDocument.SigningKeys,
            ValidateIssuerSigningKey = true,
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(idToken, tokenValidationParameters, out _);

        return principal;
    }
    
    private async Task<OpenIdConnectMessage> RefreshTokenAsync(string refreshToken)
    {

        var discoveryDocument = await oidcConfigurationManager.GetConfigurationAsync();

        var msg = new
        {
            grant_type = IOpenIdConnectService.RefreshToken,
            refresh_token = refreshToken,
            client_id = openIdConnectConfig.ClientId,
            client_secret = openIdConnectConfig.Secret,
        };

        var json = await discoveryDocument.TokenEndpoint
            .AllowAnyHttpStatus()
            .PostUrlEncodedAsync(msg)
            .ReceiveString();

        return new OpenIdConnectMessage(json);
    }
}