using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API.Content;
using Mnema.Common;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Bato;
using Mnema.Providers.Dynasty;
using Mnema.Providers.Mangadex;
using Mnema.Providers.Webtoon;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{

    public static IServiceCollection AddProviders(this IServiceCollection services)
    {
        #region Nyaa

        services.AddKeyedSingleton<IContentManager, NoOpContentManager>(Provider.Nyaa);

        #endregion

        #region Mangadex

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Mangadex);
        services.AddKeyedScoped<IRepository, MangadexRepository>(Provider.Mangadex);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Mangadex);
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
        services.AddKeyedScoped<IRepository, WebtoonRepository>(Provider.Webtoons);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Webtoons);
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
        services.AddKeyedScoped<IRepository, DynastyRepository>(Provider.Dynasty);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Dynasty);
        services.AddHttpClient(nameof(Provider.Dynasty), client =>
        {
            client.BaseAddress = new Uri("https://dynasty-scans.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region Bato

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Bato);
        services.AddKeyedScoped<IRepository, BatoRepository>(Provider.Bato);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Bato);
        services.AddHttpClient(nameof(Provider.Bato), client =>
        {
            client.BaseAddress = new Uri("https://jto.to");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
        });

        #endregion

        #region MangaBuddy

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.MangaBuddy);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.MangaBuddy);

        #endregion

        return services;
    }
    
}