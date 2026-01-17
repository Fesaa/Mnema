using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.External;
using Mnema.Models.Entities.External;
using Mnema.Services.External;
using Mnema.Services.Hubs;
using Mnema.Services.Scheduled;
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
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<ISubscriptionScheduler, SubscriptionScheduler>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IDownloadClientService, DownloadClientService>();
        services.AddScoped<IParserService, ParserService>();
        services.AddScoped<INamingService, NamingService>();

        #region External Connection

        services.AddScoped<IExternalConnectionService, ExternalConnectionService>();
        services.AddKeyedScoped<IExternalConnectionHandlerService, DiscordExternalConnectionService>(
            ExternalConnectionType.Discord);
        services.AddKeyedScoped<IExternalConnectionHandlerService, KavitaExternalConnectionService>(
            ExternalConnectionType.Kavita);

        #endregion

        return services;
    }

    public static void MapMnema(this IEndpointRouteBuilder builder)
    {
        builder.MapHub<MessageHub>("/ws");
    }
}
