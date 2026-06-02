using System;
using System.Net.Http;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Cleanup;
using Mnema.Providers.Dynasty;
using Mnema.Providers.Kagane;
using Mnema.Providers.Managers.Publication;
using Mnema.Providers.Managers.QBit;
using Mnema.Providers.Mangadex;
using Mnema.Providers.Nyaa;
using Mnema.Providers.Repositories.AthreaScans;
using Mnema.Providers.Repositories.Madokami;
using Mnema.Providers.Services;
using Mnema.Providers.Webtoon;
using QBitContentManager = Mnema.Providers.Managers.QBit.QBitContentManager;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{
    extension(IServiceCollection services)
    {
        public void AddProviders()
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

            services.AddHttpClient(nameof(Provider.Nyaa), ConfigureDefaultClient("https://nyaa.si"));

            #endregion

            #region Mangadex

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Mangadex);
            services.AddRepository<MangadexRepository>(Provider.Mangadex);
            services.AddKeyedScoped<IPreDownloadHook, LoadVolumesHook>(Provider.Mangadex);
            services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Mangadex);

            services.AddHttpClient(nameof(Provider.Mangadex), ConfigureDefaultClient("https://api.mangadex.org"));

            #endregion

            #region Weebdex

            services.AddKeyedSingleton<IContentManager, NoOpContentManager>(Provider.Weebdex);
            services.AddRepository<NoOpRepository>(Provider.Weebdex);

            #endregion

            #region Webtoons

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Webtoons);
            services.AddRepository<WebtoonRepository>(Provider.Webtoons);
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
            services.AddRepository<DynastyRepository>(Provider.Dynasty);
            services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Dynasty);

            services.AddHttpClient(nameof(Provider.Dynasty), ConfigureDefaultClient("https://dynasty-scans.com/"));

            services.AddScoped<IScheduled, ProviderJobScheduler>();

            #endregion

            #region Bato

            services.AddKeyedSingleton<IContentManager, NoOpContentManager>(Provider.Bato);
            services.AddRepository<NoOpRepository>(Provider.Bato);

            #endregion

            #region Comix

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Comix);
            services.AddRepository<NoOpRepository>(Provider.Comix);

            #endregion

            #region Kagane

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Kagane);
            services.AddRepository<KaganeRepository>(Provider.Kagane);
            services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.Kagane);

            var kaganeLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(1),
                QueueLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });

            services.AddTransient(_ => new RateLimitingHandler(kaganeLimiter));
            services.AddHttpClient(nameof(Provider.Kagane), ConfigureDefaultClient("https://yuzuki.kagane.to"))
                .AddHttpMessageHandler<RateLimitingHandler>();

            #endregion

            #region Madokami

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.MadoKami);
            services.AddRepository<MadokamiRepository>(Provider.MadoKami);
            services.AddKeyedScoped<IIoHandler, FileIoWorker>(Provider.MadoKami);

            services.AddTransient<MadokamiBasicAuthHandler>();
            services.AddHttpClient(nameof(Provider.MadoKami), ConfigureDefaultClient("https://manga.madokami.al/"))
                .AddHttpMessageHandler<MadokamiBasicAuthHandler>();

            services.AddKeyedScoped<IConfigurationProvider, MadokamiRepository>(DownloadClientType.Madokami);

            #endregion

            #region AthreaScans

            services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.AthreaScans);
            services.AddRepository<AthreaScansRepository>(Provider.AthreaScans);
            services.AddKeyedScoped<IIoHandler, ImageIoWorker>(Provider.AthreaScans);
            services.AddHttpClient(nameof(Provider.AthreaScans), ConfigureDefaultClient("https://athreascans.com/"));

            #endregion
        }

        private void AddRepository<T>(Provider provider)
            where T : class, IRepository
        {
            services.AddScoped<T>();
            services.AddKeyedScoped<IContentRepository>(provider,
                (s, _) => s.GetRequiredService<T>());
            services.AddKeyedScoped<IRepository>(provider,
                (s, _) => s.GetRequiredService<T>());
        }
    }

    private static Action<HttpClient> ConfigureDefaultClient(string uri)
    {
        return client =>
        {
            client.BaseAddress = new Uri(uri);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        };
    }
}
