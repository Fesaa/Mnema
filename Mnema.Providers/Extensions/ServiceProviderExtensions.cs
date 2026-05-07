using System;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Cleanup;
using Mnema.Providers.Comix;
using Mnema.Providers.Dynasty;
using Mnema.Providers.Kagane;
using Mnema.Providers.Managers.Publication;
using Mnema.Providers.Managers.QBit;
using Mnema.Providers.Mangadex;
using Mnema.Providers.Nyaa;
using Mnema.Providers.Repositories.Madokami;
using Mnema.Providers.Services;
using Mnema.Providers.Webtoon;
using QBitContentManager = Mnema.Providers.Managers.QBit.QBitContentManager;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddProviders(this IServiceCollection services)
    {
        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IScannerService, ScannerService>();
        services.AddScoped<ICleanupService, CleanupService>();
        services.AddScoped<PublicationCleanupService>();
        services.AddScoped<RawFileCleanupService>();
        services.AddScoped<IFormatHandler, ArchiveFormatHandler>();
        services.AddScoped<IFormatHandler, EpubFormatHandler>();
        services.AddScoped<NoOpRepository>();

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
        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Mangadex);
        services.AddHttpClient(nameof(Provider.Mangadex), client =>
        {
            client.BaseAddress = new Uri("https://api.mangadex.org");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Weebdex

        services.AddKeyedSingleton<IContentManager, NoOpContentManager>(Provider.Weebdex);

        services.AddKeyedScoped<IContentRepository, NoOpRepository>(Provider.Weebdex);
        services.AddKeyedScoped<IRepository, NoOpRepository>(Provider.Weebdex);
        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Weebdex);
        services.AddHttpClient(nameof(Provider.Weebdex), client =>
        {
            client.BaseAddress = new Uri("https://api.weebdex.org");
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

        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Webtoons);
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

        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Dynasty);
        services.AddHttpClient(nameof(Provider.Dynasty), client =>
        {
            client.BaseAddress = new Uri("https://dynasty-scans.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Bato

        services.AddKeyedSingleton<IContentManager, NoOpContentManager>(Provider.Bato);

        services.AddKeyedScoped<IContentRepository, NoOpRepository>(Provider.Bato);
        services.AddKeyedScoped<IRepository, NoOpRepository>(Provider.Bato);

        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Bato);
        services.AddHttpClient(nameof(Provider.Bato), client =>
        {
            client.BaseAddress = new Uri("https://xbat.app");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Comix

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Comix);

        services.AddScoped<ComixRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Comix,
            (s, _) => s.GetRequiredService<ComixRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Comix,
            (s, _) => s.GetRequiredService<ComixRepository>());

        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Comix);
        services.AddHttpClient(nameof(Provider.Comix), client =>
        {
            client.BaseAddress = new Uri("https://comix.to");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
            client.DefaultRequestHeaders.Add(HeaderNames.Referer, "https://comix.to/");
        });

        #endregion

        #region Kagane

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Kagane);

        services.AddScoped<KaganeRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.Kagane,
            (s, _) => s.GetRequiredService<KaganeRepository>());
        services.AddKeyedScoped<IRepository>(Provider.Kagane,
            (s, _) => s.GetRequiredService<KaganeRepository>());

        services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Kagane);
        var kaganeLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromSeconds(1),
            QueueLimit = 100,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        services.AddTransient(_ => new RateLimitingHandler(kaganeLimiter));
        services.AddHttpClient(nameof(Provider.Kagane), client =>
        {
            client.BaseAddress = new Uri("https://yuzuki.kagane.org");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        }).AddHttpMessageHandler<RateLimitingHandler>();

        #endregion

        #region Madokami

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.MadoKami);

        services.AddScoped<MadokamiRepository>();
        services.AddKeyedScoped<IContentRepository>(Provider.MadoKami,
            (s, _) => s.GetRequiredService<MadokamiRepository>());
        services.AddKeyedScoped<IRepository>(Provider.MadoKami,
            (s, _) => s.GetRequiredService<MadokamiRepository>());

        services.AddKeyedScoped<IIoHandler, FileIoWorker>(Provider.MadoKami);
        services.AddTransient<MadokamiBasicAuthHandler>();
        services.AddHttpClient(nameof(Provider.MadoKami), client =>
        {
            client.BaseAddress = new Uri("https://manga.madokami.al/ ");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        }).AddHttpMessageHandler<MadokamiBasicAuthHandler>();

        services.AddKeyedScoped<IConfigurationProvider, MadokamiRepository>(DownloadClientType.Madokami);

        #endregion

        return services;
    }
}
