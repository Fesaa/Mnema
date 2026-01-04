using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.Internal;

namespace Mnema.Server.Helpers;

public class OpenIdConnectEventHelper : OpenIdConnectEvents
{
    private readonly bool _isDevelopment;

    public OpenIdConnectEventHelper(bool isDevelopment)
    {
        _isDevelopment = isDevelopment;

        OnTokenValidated = HandleOnTokenValidated;
        OnRemoteFailure = HandleRemoteFailure;
        OnRedirectToIdentityProvider = HandleOnRedirectToIdentityProvider;
        OnRedirectToIdentityProviderForSignOut = HandleOnRedirectToIdentityProviderForSignOut;
    }

    private Task HandleRemoteFailure(RemoteFailureContext ctx)
    {
        if (ctx.Failure == null)
            return Task.CompletedTask;

        var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<IOpenIdConnectService>>();

        logger.LogError(ctx.Failure, "Encountered an exception while communicating with the idp");
        ctx.Response.Redirect("/login?skipAutoLogin=true&error=" + Uri.EscapeDataString(ctx.Failure.Message));
        ctx.HandleResponse();

        return Task.CompletedTask;
    }

    private Task HandleOnRedirectToIdentityProvider(RedirectContext ctx)
    {
        // Intercept redirects on API requests and instead return 401
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/hubs"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.HandleResponse();
            return Task.CompletedTask;
        }

        if (!_isDevelopment && !string.IsNullOrEmpty(ctx.ProtocolMessage.RedirectUri))
            ctx.ProtocolMessage.RedirectUri = ctx.ProtocolMessage.RedirectUri.Replace("http://", "https://");

        return Task.CompletedTask;
    }

    private Task HandleOnRedirectToIdentityProviderForSignOut(RedirectContext ctx)
    {
        if (!_isDevelopment && !string.IsNullOrEmpty(ctx.ProtocolMessage.PostLogoutRedirectUri))
            ctx.ProtocolMessage.PostLogoutRedirectUri =
                ctx.ProtocolMessage.PostLogoutRedirectUri.Replace("http://", "https://");

        return Task.CompletedTask;
    }

    private static async Task HandleOnTokenValidated(TokenValidatedContext ctx)
    {
        try
        {
            if (ctx.Principal == null) return;

            var unitOfWork = ctx.HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var oidcService = ctx.HttpContext.RequestServices.GetRequiredService<IOpenIdConnectService>();

            var externalId = ctx.Principal.FindFirst(ClaimTypes.NameIdentifier);
            if (externalId == null) throw new UnauthorizedAccessException();

            if (!Guid.TryParse(externalId.Value, out var guid)) throw new MnemaException("Id is not a Guid");

            var roles = ctx.Principal.FindAll(ClaimTypes.Role).Where(c => Roles.AllRoles.Contains(c.Value));
            if (!roles.Any()) throw new UnauthorizedAccessException();

            // Ensure user is created
            await unitOfWork.UserRepository.GetUserById(guid);

            var tokens = CopyOidcTokens(ctx);
            ctx.Properties ??= new AuthenticationProperties();
            ctx.Properties.StoreTokens(tokens);


            var idToken = ctx.Properties.GetTokenValue(IOpenIdConnectService.IdToken);
            if (!string.IsNullOrEmpty(idToken)) ctx.Principal = await oidcService.ParseIdToken(idToken);

            ctx.Success();
        }
        catch (Exception ex)
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<IOpenIdConnectService>>();
            logger.LogError(ex, "An exception occured during initial OIDC flow");

            ctx.Response.Redirect("/login?skipAutoLogin=true&error=" + Uri.EscapeDataString(ex.Message));
            ctx.HandleResponse();
        }
    }

    private static List<AuthenticationToken> CopyOidcTokens(TokenValidatedContext ctx)
    {
        if (ctx.TokenEndpointResponse == null) return [];

        var tokens = new List<AuthenticationToken>();

        if (!string.IsNullOrEmpty(ctx.TokenEndpointResponse.RefreshToken))
        {
            tokens.Add(new AuthenticationToken
                { Name = IOpenIdConnectService.RefreshToken, Value = ctx.TokenEndpointResponse.RefreshToken });
        }
        else
        {
            var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<IOpenIdConnectService>>();
            logger.LogWarning("OIDC login without refresh token, automatic sync will not work for this user");
        }

        if (!string.IsNullOrEmpty(ctx.TokenEndpointResponse.IdToken))
            tokens.Add(new AuthenticationToken
                { Name = IOpenIdConnectService.IdToken, Value = ctx.TokenEndpointResponse.IdToken });

        if (!string.IsNullOrEmpty(ctx.TokenEndpointResponse.ExpiresIn))
        {
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(double.Parse(ctx.TokenEndpointResponse.ExpiresIn));
            tokens.Add(new AuthenticationToken
                { Name = IOpenIdConnectService.ExpiresAt, Value = expiresAt.ToString("o") });
        }

        return tokens;
    }
}