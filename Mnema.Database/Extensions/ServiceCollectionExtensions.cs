using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Models.Internal;

namespace Mnema.Database.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddMnemaPostgresDatabase(IConfiguration configuration, bool isDevelopment)
        {
            return serviceCollection.AddDbContextFactory<MnemaDataContext>(options => options
                .UseNpgsql(configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey), b => b
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging(isDevelopment)

            );
        }

        public IServiceCollection AddDatabaseServices()
        {
            return serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
