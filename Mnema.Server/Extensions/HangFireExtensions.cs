using System;
using System.Linq;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mnema.API.External;
using Mnema.Models.Internal;
using Mnema.Server.Helpers;

namespace Mnema.Server.Extensions;

public static class HangFireExtensions
{
    extension(IServiceCollection services)
    {
        public void AddAndConfigureHangFire(IConfiguration configuration)
        {
            var psql = configuration.GetConnectionString(ConfigurationKeys.PostgresConnectionKey);

            services.AddHangfire(config =>
            {
                config.UseFilter(new ExceptionJobFilter());

                if (!string.IsNullOrEmpty(psql))
                {
                    config.UsePostgreSqlStorage(
                            options => { options.UseNpgsqlConnection(psql); },
                            new PostgreSqlStorageOptions
                            {
                                SchemaName = "HangFire",
                                PrepareSchemaIfNecessary = true,
                                QueuePollInterval = TimeSpan.FromSeconds(15)
                            })
                        .UseSerilogLogProvider();
                    return;
                }

                config.UseInMemoryStorage()
                    .UseSerilogLogProvider();
            });
            services.AddHangfireServer(options =>
            {
                options.Queues = HangfireQueue.Queues.ToArray();
            });
        }
    }
}
