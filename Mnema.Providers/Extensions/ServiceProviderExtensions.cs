using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Bato;
using Mnema.Providers.Mangadex;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{

    public static IServiceCollection AddProviders(this IServiceCollection services)
    {

        #region Mangadex

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Mangadex);
        services.AddKeyedScoped<IRepository, MangadexRepository>(Provider.Mangadex);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Mangadex);
        services.AddKeyedScoped<IPreDownloadHook, LoadVolumesHook>(Provider.Mangadex);
        services.AddHttpClient(nameof(Provider.Mangadex), client =>
        {
            client.BaseAddress = new Uri("https://api.mangadex.org");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mnema");
        });

        #endregion

        #region Webtoons

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Webtoons);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Webtoons);

        #endregion

        #region Dynasty

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Dynasty);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Dynasty);

        #endregion

        #region Bato

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.Bato);
        services.AddKeyedScoped<IRepository, BatoRepository>(Provider.Bato);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.Bato);
        services.AddHttpClient(nameof(Provider.Bato), client =>
        {
            client.BaseAddress = new Uri("https://jto.to");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mnema");
        });

        #endregion

        #region MangaBuddy

        services.AddKeyedSingleton<IContentManager, PublicationManager>(Provider.MangaBuddy);
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(Provider.MangaBuddy);

        #endregion

        return services;
    }
    
}