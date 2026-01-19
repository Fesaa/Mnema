using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Metadata.Hardcover;
using Mnema.Metadata.Mangabaka;
using Mnema.Models.Entities.Content;
using Mnema.Models.Internal;
using Serilog;

namespace Mnema.Metadata.Extensions;

public static class ServiceProviderExtensions
{

    private const string HardcoverGraphQlEndpoint = "https://api.hardcover.app/v1/graphql";

    public static IServiceCollection AddMetadataProviders(this IServiceCollection services, IConfiguration cfg, ApplicationConfiguration configuration)
    {

        var hardCoverToken = cfg.GetRequiredSection("Authentication").GetValue<string>("Hardcover");
        if (!string.IsNullOrEmpty(hardCoverToken))
        {
            services.AddKeyedScoped<IMetadataProviderService, HardcoverMetadataService>(MetadataProvider.Hardcover);
            services.AddHttpClient(nameof(HardcoverMetadataService), client =>
            {
                client.BaseAddress = new Uri(HardcoverGraphQlEndpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, "Mnema");
                client.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Bearer {hardCoverToken}");
            });
            services.AddKeyedSingleton<IGraphQLClient>(MetadataProvider.Hardcover,(s, _) =>
            {
                var httpClient = s.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(HardcoverMetadataService));

                return new GraphQLHttpClient(HardcoverGraphQlEndpoint,
                    new GraphQL.Client.Serializer.SystemTextJson.SystemTextJsonSerializer
                    {
                        Options =
                        {
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                            //Converters = { new JsonNumberEnumConverter<HardcoverUserBookStatus>() }
                        }
                    }, httpClient);
            });
        }
        else
        {
            Log.Logger.Warning($"No authentication token configured for {nameof(MetadataProvider.Hardcover)}, hardcover services will not be avaible");
        }

        services.AddScoped<IScheduled, MangabakaScheduler>();
        services.AddKeyedScoped<IMetadataProviderService, MangabakaMetadataService>(MetadataProvider.Mangabaka);

        var connectionString = $"Data Source={Path.Join(configuration.PersistentStorage, MangabakaScheduler.DatabaseName)}";
        services.AddDbContextPool<MangabakaDbContext>(options =>
        {
            options.UseSqlite(connectionString, builder =>
            {
                builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        });

        return services;
    }

}
