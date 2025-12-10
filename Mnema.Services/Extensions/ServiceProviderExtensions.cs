using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Services.Store;

namespace Mnema.Services.Extensions;

public static class ServiceProviderExtensions
{

    public static IServiceCollection AddMnemaServices(this IServiceCollection services)
    {
        services.AddSingleton<ITicketStore, CustomTicketStore>();
        services.AddScoped<IOpenIdConnectService, OpenIdConnectService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPagesService, PageService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
    
}