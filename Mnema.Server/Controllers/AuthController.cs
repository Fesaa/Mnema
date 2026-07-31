using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnema.API.Services;
using Mnema.Models.Internal;

namespace Mnema.Server.Controllers;

public class AuthController(IPasswordService passwordService) : BaseApiController
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login()
    {
        var form = await Request.ReadFormAsync();
        if (!form.TryGetValue("password", out var password))
        {
            return Redirect("/login.html?error=invalid_password");
        }

        if (!await passwordService.VerifyHashedPassword(password.FirstOrDefault() ?? string.Empty))
        {
            return Redirect("/login.html?error=invalid_password");
        }

        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaims(Roles.AllRoles.Select(r => new Claim(ClaimTypes.Role, r)));
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, });

        return Redirect("/");
    }

    [AllowAnonymous]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/");
    }
}
