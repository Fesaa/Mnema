using Microsoft.Extensions.DependencyInjection;
using Mnema.API.Content;
using Mnema.Models.Entities.Content;
using Mnema.Providers.Mangadex;

namespace Mnema.Providers.Extensions;

public static class ServiceProviderExtensions
{

    public static IServiceCollection AddProviders(this IServiceCollection services)
    {

        #region Mangadex

        services.AddKeyedSingleton<IContentManager, PublicationManager>(nameof(Provider.Mangadex));
        services.AddKeyedScoped<IRepository, MangadexRepository>(nameof(Provider.Mangadex));
        services.AddKeyedScoped<IPublicationExtensions, MangaPublicationExtensions>(nameof(Provider.Mangadex));
        services.AddHttpClient(nameof(Provider.Mangadex), client =>
        {
            client.BaseAddress = new Uri("https://api.mangadex.org");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "Mnema");
        });

        #endregion
        

        return services;
    }
    
}