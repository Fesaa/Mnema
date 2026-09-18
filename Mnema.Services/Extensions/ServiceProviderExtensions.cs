using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.Services;
using Mnema.Common.Extensions;
using Mnema.Models.Entities;
using Mnema.Services.Connections;
using Mnema.Services.Hubs;
using Mnema.Services.Scheduled;
using Mnema.Services.Store;

namespace Mnema.Services.Extensions;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddMnemaServices(this IServiceCollection services, bool authDisabled)
    {
        if (!authDisabled)
        {
            services.AddSingleton<TicketSerializer>();
            services.AddSingleton<ITicketStore, CustomTicketStore>();
        }

        services.AddScheduled();

        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IPagesService, PageService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDownloadService, DownloadService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IDownloadClientService, DownloadClientService>();
        services.AddScoped<IParserService, ParserService>();
        services.AddScoped<INamingService, NamingService>();
        services.AddScoped<IMetadataResolver, MetadataResolver>();
        services.AddScoped<IMonitoredSeriesService, MonitoredSeriesService>();
        services.AddScoped<IAuthKeyService, AuthKeyService>();
        services.AddScoped<ICalendarService, CalendarService>();
        services.AddScoped<IProviderSettingsService, ProviderSettingsService>();
        services.AddScoped<IGroupedReleaseDetector, GroupedReleaseDetector>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IImportScanService, ImportScanService>();
        services.AddScoped<IEpubMetadataService, EpubMetadataService>();

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

    private static void AddScheduled(this IServiceCollection services)
    {
        var scheduledTypes = Assembly.GetAssembly(typeof(ServiceProviderExtensions))?
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(IScheduled).IsAssignableFrom(type))
            .ToList();

        foreach (var type in (scheduledTypes ?? []).Where(type => typeof(IScheduled).IsAssignableFrom(type)))
        {
            services.AddScoped(typeof(IScheduled), type);
        }
    }
}
