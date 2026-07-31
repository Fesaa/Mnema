using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Database;
using Mnema.Models.Internal;
using Mnema.Server.Helpers;
using Mnema.Server.Middleware;

namespace Mnema.Server.Extensions;

public static class AuthenticationExtensions
{
    private const string DynamicHybrid = nameof(DynamicHybrid);
    public const string OpenIdConnect = nameof(OpenIdConnect);
    public const string NoAuthentication = nameof(NoAuthentication);

    public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection()
            .PersistKeysToDbContext<MnemaDataContext>()
            .SetApplicationName("Mnema");

        services.AddAuthorizationBuilder()
            .AddPolicy(Roles.Subscriptions)
            .AddPolicy(Roles.ManageSettings)
            .AddPolicy(Roles.ManagePages)
            .AddPolicy(Roles.HangFire)
            .AddPolicy(Roles.CreateDirectory)
            .AddPolicy(Roles.ManageExternalConnections);

        var noAuthEnabled = configuration.GetSection(NoAuthentication).Get<bool>();
        if (noAuthEnabled)
        {
            services.AddAuthentication(NoAuthAuthenticationSchemeOptions.SchemeName)
                .AddScheme<NoAuthAuthenticationSchemeOptions, NoAuthAuthenticationHandler>(NoAuthAuthenticationSchemeOptions.SchemeName, null);

            return services;
        }

        var auth = services.AddAuthentication(DynamicHybrid);

        auth.AddPolicyScheme(DynamicHybrid, DynamicHybrid, options =>
        {
            options.ForwardDefaultSelector = ctx =>
            {
                if (ctx.Request.Query.ContainsKey(AuthKeyAuthenticationSchemeOptions.AuthKeyQueryKey))
                {
                    return AuthKeyAuthenticationSchemeOptions.SchemeName;
                }

                return CookieAuthenticationDefaults.AuthenticationScheme;
            };
        });

        auth.AddScheme<AuthKeyAuthenticationSchemeOptions, AuthKeyAuthenticationHandler>(AuthKeyAuthenticationSchemeOptions.SchemeName, null);

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) =>
            {
                options.Cookie.Name = "Mnema.Auth";
                options.LoginPath = "/login.html";
                options.AccessDeniedPath = "/access-denied.html";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
                options.SessionStore = store;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;

                options.Events.OnRedirectToLogin = ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/api"))
                    {
                        ctx.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }

                    ctx.Response.Redirect(ctx.RedirectUri);
                    return Task.CompletedTask;
                };
            });

        auth.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        return services;
    }

    private static AuthorizationBuilder AddPolicy(this AuthorizationBuilder builder, string roleName)
    {
        return builder.AddPolicy(roleName, policy =>
            policy.RequireRole(roleName, roleName.ToLower(), roleName.ToUpper()));
    }
}
