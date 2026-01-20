using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Bato;
using Mnema.Providers.Cleanup;
using Mnema.Providers.Dynasty;
using Mnema.Providers.Mangadex;
using Mnema.Providers.Nyaa;
using Mnema.Providers.QBit;
using Mnema.Providers.Services;
using Mnema.Providers.Webtoon;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddProviders(this IServiceCollection services)
    {
        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ICleanupService, CleanupService>();
        services.AddScoped<PublicationCleanupService>();
        services.AddScoped<TorrentCleanupService>();
        services.AddScoped<IFormatHandler, ArchiveFormatHandler>();
        services.AddScoped<IFormatHandler, EpubFormatHandler>();

        #region qBit Torrent

        services.AddSingleton<IQBitClient, QBitClient>();
        services.AddSingleton<QBitContentManager>();
        services.AddKeyedSingleton<IConfigurationProvider>(DownloadClientType.QBittorrent,
            (s, _) => s.GetRequiredService<QBitContentManager>());
        services.AddHostedService<TorrentWatcherService>();

        #endregion

        #region Nyaa

        services.AddKeyedSingleton<IContentManager>(Provider.Nyaa,
            (s, _) => s.GetRequiredService<QBitContentManager>());
        services.AddScoped<NyaaRepository>();

        services.AddKeyedScoped<IContentRepository>(Provider.Nyaa,
            (s, _) => s.GetRequiredService<NyaaRepository>());

        services.AddHttpClient(nameof(Provider.Nyaa), client =>
        {
            client.BaseAddress = new Uri("https://nyaa.si");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Mangadex

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Mangadex);

        services.AddScoped<MangadexRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Mangadex,
            (s, _) => s.GetRequiredService<MangadexRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Mangadex,
            (s, _) => s.GetRequiredService<MangadexRepository>());

        services.AddKeyedScoped<IPreDownloadHook, LoadVolumesHook>(Provider.Mangadex);
        services.AddHttpClient(nameof(Provider.Mangadex), client =>
        {
            client.BaseAddress = new Uri("https://api.mangadex.org");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Webtoons

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Webtoons);

        services.AddScoped<WebtoonRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Webtoons,
            (s, _) => s.GetRequiredService<WebtoonRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Webtoons,
            (s, _) => s.GetRequiredService<WebtoonRepository>());

        services.AddHttpClient(nameof(Provider.Webtoons), client =>
        {
            client.BaseAddress = new Uri("https://www.webtoons.com");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
            client.DefaultRequestHeaders.Add(HeaderNames.Referer, "https://www.webtoons.com/");
        });

        #endregion

        #region Dynasty

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Dynasty);

        services.AddScoped<DynastyRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Dynasty,
            (s, _) => s.GetRequiredService<DynastyRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Dynasty,
            (s, _) => s.GetRequiredService<DynastyRepository>());

        services.AddHttpClient(nameof(Provider.Dynasty), client =>
        {
            client.BaseAddress = new Uri("https://dynasty-scans.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Bato

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Bato);

        services.AddScoped<BatoRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Bato,
            (s, _) => s.GetRequiredService<BatoRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Bato,
            (s, _) => s.GetRequiredService<BatoRepository>());

        services.AddHttpClient(nameof(Provider.Bato), client =>
        {
            client.BaseAddress = new Uri("https://jto.to");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        return services;
    }
}
