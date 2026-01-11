using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API.Content;
using Mnema.Metadata.Hardcover;
using Mnema.Models.Entities.Content;
using Serilog;

namespace Mnema.Metadata.Extensions;

public static class ServiceProviderExtensions
{

    public const string HardcoverGraphQlEndpoint = "https://api.hardcover.app/v1/graphql";

    public static IServiceCollection AddMetadataProviders(this IServiceCollection services, IConfiguration cfg)
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

        return services;
    }

}
