using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Mnema.Common.Exceptions;
using Mnema.Models.Internal;
using Mnema.Server.Helpers;

namespace Mnema.Server.Extensions;

public static class OpenIdConnectServiceExtensions
{
    public const string OpenIdConnect = nameof(OpenIdConnect);

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var openIdConnectConfig = configuration.GetSection(OpenIdConnect).Get<OpenIdConnectConfig>();
        if (openIdConnectConfig is not {Valid: true})
        {
            throw new MnemaException("No valid OpenIDConnect configuration found");
        }
        
        services.AddSingleton<ConfigurationManager<OpenIdConnectConfiguration>>(_ =>
        {
            var url = openIdConnectConfig.Authority + "/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                url,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = url.StartsWith("https") }
            );
        });
        services.AddSingleton<OpenIdConnectConfig>(_ => openIdConnectConfig);
        services.AddSingleton<TicketSerializer>();

        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) =>
            {
                
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.MaxAge = TimeSpan.FromDays(30);
                options.SessionStore = store;

                options.LoginPath = "/Auth/login";
                options.LogoutPath = "/Auth/logout";
                
                if (environment.IsDevelopment())
                {
                    options.Cookie.Domain = null;
                }

                options.Events = new CookieAuthenticationEventsHelper();
            });

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddOpenIdConnect(OpenIdConnect, options =>
            {
                options.Authority = openIdConnectConfig.Authority;
                options.ClientId = openIdConnectConfig.ClientId;
                options.ClientSecret = openIdConnectConfig.Secret;
                options.RequireHttpsMetadata = options.Authority.StartsWith("https://");
                
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("offline_access");
                options.Scope.Add("roles");
                options.Scope.Add("email");

                options.Events = new OpenIdConnectEventHelper(environment.IsDevelopment());
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Roles.ManagePages);

        return services;
    }
    
    private static AuthorizationBuilder AddPolicy(this AuthorizationBuilder builder, string roleName)
    {
        return builder.AddPolicy(roleName, policy => 
            policy.RequireRole(roleName, roleName.ToLower(), roleName.ToUpper()));
    }
    
}