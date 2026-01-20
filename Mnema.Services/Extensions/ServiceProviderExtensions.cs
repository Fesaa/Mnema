using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities;
using Mnema.Services.Connections;
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
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<IScheduled, MonitoredSeriesScheduler>();
        services.AddScoped<IScheduled, MonitoredSeriesMetadataScheduler>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IDownloadClientService, DownloadClientService>();
        services.AddScoped<IParserService, ParserService>();
        services.AddScoped<INamingService, NamingService>();
        services.AddScoped<IMetadataResolver, MetadataResolver>();
        services.AddScoped<IMonitoredSeriesService, MonitoredSeriesService>();

        #region External Connection

        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddKeyedScoped<IConnectionHandlerService, DiscordConnectionService>(
            ConnectionType.Discord);
        services.AddKeyedScoped<IConnectionHandlerService, KavitaConnectionService>(
            ConnectionType.Kavita);
        services.AddKeyedScoped<IConnectionHandlerService, NativeConnectionService>(
            ConnectionType.Native);

        #endregion

        return services;
    }

    public static void MapMnema(this IEndpointRouteBuilder builder)
    {
        builder.MapHub<MessageHub>("/ws");
    }
}
