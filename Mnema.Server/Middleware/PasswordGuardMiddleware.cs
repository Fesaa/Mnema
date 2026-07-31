using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Models.Entities;

namespace Mnema.Server.Middleware;

public class PasswordGuardMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var unitOfWork = context.RequestServices.GetRequiredService<IUnitOfWork>();

        var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
        var passwordSetting = await unitOfWork.SettingsRepository.GetSettingsAsync(ServerSettingKey.Password);

        if (context.Request.Path.Equals("/login.html", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(passwordSetting.Value))
            {
                context.Response.Redirect("/first-setup.html");
                return;
            }

            if (isAuthenticated)
            {
                context.Response.Redirect("/");
                return;
            }
        }
        else if (context.Request.Path.Equals("/first-setup.html", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(passwordSetting.Value))
            {
                context.Response.Redirect("/login.html");
                return;
            }
        }

        await next(context);
    }
}
