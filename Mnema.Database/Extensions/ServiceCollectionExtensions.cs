using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API;
using Mnema.Database.Interceptors;
using Mnema.Models.Internal;

namespace Mnema.Database.Extensions;

public static class ServiceCollectionExtensions
{

    extension(IServiceCollection serviceCollection)
    {

        public IServiceCollection AddMnemaPostgresDatabase(IConfiguration configuration, bool isDevelopment)
            => serviceCollection.AddDbContextFactory<MnemaDataContext>(options =>  options
                .UseNpgsql(configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey), b => b
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging(isDevelopment)
                .AddInterceptors(new TimeAuditableInterceptor())
            );

        public IServiceCollection AddDatabaseServices() => serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();

    }
    
}