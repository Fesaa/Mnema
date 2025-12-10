using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Mnema.API;

namespace Mnema.Server.Helpers;

public class CookieAuthenticationEventsHelper: CookieAuthenticationEvents
{
    public CookieAuthenticationEventsHelper()
    {
        OnValidatePrincipal = HandleOnValidatePrincipal;
        OnRedirectToAccessDenied = HandleOnRedirectToAccessDenied;
        OnRedirectToLogin = HandleOnRedirectToLogin;
    }

    private static async Task HandleOnValidatePrincipal(CookieValidatePrincipalContext ctx)
    {
        var openIdConnectService = ctx.HttpContext.RequestServices.GetRequiredService<IOpenIdConnectService>();

        await openIdConnectService.RefreshCookieToken(ctx);
    }

    private static Task HandleOnRedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    private static Task HandleOnRedirectToLogin(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/hubs"))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        
        ctx.Response.Redirect($"/Auth/login?returnUrl={Uri.EscapeDataString(ctx.Request.Path)}");
        
        return Task.CompletedTask;
    }
}