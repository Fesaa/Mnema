using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Mnema.API;
using Mnema.API.Content;
using Mnema.API.Metadata;
using Mnema.Common;
using Mnema.Metadata.Hardcover;
using Mnema.Metadata.Mangabaka;
using Mnema.Models.Entities.Content;
using Mnema.Models.Enums;
using Mnema.Models.Internal;
using Serilog;
using Directory = System.IO.Directory;
using IConfigurationProvider = Mnema.API.IConfigurationProvider;

namespace Mnema.Metadata.Extensions;

public static class ServiceProviderExtensions
{

    private const string HardcoverGraphQlEndpoint = "https://api.hardcover.app/v1/graphql";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddMetadataProviders(IConfiguration cfg, ApplicationConfiguration configuration)
            => services
                .AddHardcoverServices(cfg, configuration)
                .AddMangabakaServices(cfg, configuration);

        private IServiceCollection AddHardcoverServices(IConfiguration cfg, ApplicationConfiguration configuration)
        {
            services.AddKeyedScoped<IConfigurationProvider, HardcoverMetadataConfiguration>(MetadataProvider.Hardcover);

            var hardCoverToken = cfg.GetSection("Authentication").GetValue<string>("Hardcover");
            if (string.IsNullOrEmpty(hardCoverToken))
            {
                Log.Logger.Warning($"No authentication token configured for {nameof(MetadataProvider.Hardcover)}, hardcover services will not be available");

                services.AddKeyedScoped<IMetadataProviderService, NoOpMetadataService>(MetadataProvider.Hardcover);
                return services;
            }

            services.AddKeyedScoped<IMetadataProviderService, HardcoverMetadataService>(MetadataProvider.Hardcover);
            services.AddHttpClient(nameof(HardcoverMetadataService), client =>
            {
                client.BaseAddress = new Uri(HardcoverGraphQlEndpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, BuildInfo.AppIdentifier);
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

            return services;
        }

        private IServiceCollection AddMangabakaServices(IConfiguration cfg, ApplicationConfiguration configuration)
        {
            services.AddScoped<IScheduled, MangabakaScheduler>();
            services.AddScoped<IMangabakaMetadataService, MangabakaMetadataService>();
            services.AddKeyedScoped<IMetadataProviderService>(MetadataProvider.Mangabaka,
                (s, _) => s.GetRequiredService<IMangabakaMetadataService>());
            services.AddKeyedScoped<IConfigurationProvider, MangaBakaMetadataConfiguration>(MetadataProvider.Mangabaka);

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

            var dir = Path.Join(configuration.PersistentStorage, MangabakaScheduler.LuceneIndexName);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var mangabakaLuceneDir = FSDirectory.Open(dir);

            services.AddKeyedSingleton<FSDirectory>(MetadataProvider.Mangabaka,
                (_, _)  => mangabakaLuceneDir);
            services.AddKeyedSingleton<SearcherManager>(MetadataProvider.Mangabaka, (sp, _) =>
            {
                var fsDir = sp.GetRequiredKeyedService<FSDirectory>(MetadataProvider.Mangabaka);

                if (!DirectoryReader.IndexExists(fsDir))
                {
                    var config = new IndexWriterConfig(MangabakaScheduler.Version, new StandardAnalyzer(MangabakaScheduler.Version));
                    using var writer = new IndexWriter(fsDir, config);
                    writer.Commit(); // This creates the initial 'segments' file
                }

                return new SearcherManager(fsDir, null);
            });

            services.AddHttpClient(nameof(MetadataProvider.Mangabaka), client =>
            {
                client.BaseAddress = new Uri("https://api.mangabaka.org");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, BuildInfo.AppIdentifier);
            });

            return services;
        }
    }
}
