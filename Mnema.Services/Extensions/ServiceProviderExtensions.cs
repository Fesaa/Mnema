using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Services;
using Mnema.Services.Store;

namespace Mnema.Services.Extensions;

public static class ServiceProviderExtensions
{

    public static IServiceCollection AddMnemaServices(this IServiceCollection services)
    {
        services.AddSingleton<ITicketStore, CustomTicketStore>();
        services.AddScoped<IOpenIdConnectService, OpenIdConnectService>();

        return services;
    }
    
}