using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.Database.Interceptors;

namespace Mnema.Database.Extensions;

public static class ServiceCollectionExtensions
{

    private const string PostgresConnectionKey = "Postgres";

    extension(IServiceCollection serviceCollection)
    {

        public IServiceCollection AddMnemaPostgresDatabase(IConfiguration configuration, bool isDevelopment)
            => serviceCollection.AddDbContextFactory<MnemaDataContext>(options =>  options
                .UseNpgsql(configuration.GetConnectionString(PostgresConnectionKey), b => b
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging(isDevelopment)
                .AddInterceptors(new TimeAuditableInterceptor())
            );
        
    }
    
}