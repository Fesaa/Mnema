using System;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.Models.Internal;

namespace Mnema.Server.Extensions;

public static class HangFireExtensions
{

    extension(IServiceCollection services)
    {

        public void AddAndConfigureHangFire(IConfiguration configuration)
        {
            services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey));
                }, new PostgreSqlStorageOptions
                {
                    SchemaName = "HangFire",
                    PrepareSchemaIfNecessary = true,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                });
            });
            services.AddHangfireServer();
        }
        
    }
    
}