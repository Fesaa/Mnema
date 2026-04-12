using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        public IServiceCollection AddMnemaDatabase(IConfiguration configuration)
        {
            var psql = configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey);
            var sqlite = configuration.GetConnectionString(ConfigurationKeys.SqliteConnectionKey);

            if (string.IsNullOrEmpty(psql) && string.IsNullOrEmpty(sqlite))
                throw new ArgumentException("Postgres or Sqlite connection string is required.");

            if (!string.IsNullOrEmpty(psql))
            {
                return serviceCollection.AddDbContextPool<MnemaDataContext>(options => options
                        .UseNpgsql(configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey), b => b
                            .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                        .EnableDetailedErrors()
                        .EnableSensitiveDataLogging()
                        .AddInterceptors(new NormalizationInterceptor())
                        .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                    , poolSize: 128);
            }

            serviceCollection.AddDbContextPool<SqliteMnemaDataContext>(options =>
            {
                options.UseSqlite(sqlite, b => b
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging()
                    .AddInterceptors(new NormalizationInterceptor())
                    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });

            serviceCollection.AddScoped<MnemaDataContext>(sp => sp.GetRequiredService<SqliteMnemaDataContext>());

            return serviceCollection;
        }

        public IServiceCollection AddDatabaseServices()
        {
            return serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
