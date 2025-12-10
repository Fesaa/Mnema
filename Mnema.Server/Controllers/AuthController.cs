using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Mnema.Server.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Mnema.Server.Controllers;

[Route("[controller]")]
public class AuthController : Controller
{
    [SwaggerIgnore]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = returnUrl ?? "/",
        };

        return Challenge(properties, OpenIdConnectServiceExtensions.OpenIdConnect);
    }

    [SwaggerIgnore]
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectServiceExtensions.OpenIdConnect
        );
    }
}