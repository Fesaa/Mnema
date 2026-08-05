using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mnema.Common;
using Mnema.Database;
using Mnema.Database.ManualMigrations;
using Mnema.Server.Logging;
using NetVips;
using Serilog;
using Serilog.Templates;
using Log = Serilog.Log;

namespace Mnema.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(new ExpressionTemplate(SerilogOptions.OutputTemplate))
            .MinimumLevel
            .Information()
            .CreateBootstrapLogger();

        PrintStartUp();
        InitNetVips();

        try
        {
            var host = CreateHostBuilder(args).Build();
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                var context = services.GetRequiredService<MnemaDataContext>();

                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogInformation("Migrating database to latest schema");

                    await context.Database.MigrateAsync();

                    logger.LogInformation("Database has been migrated, starting Mnema");
                }

                await new MigrateSubscriptionsToMonitoredSeries().RunAsync(services, context, logger);
                await new PinMonitoredSeriesTitles().RunAsync(services, context, logger);

                await context.SeedDatabase();

                await new MigrateMetadataProviderSettings().RunAsync(services, context, logger);
                await new SetDefaultNamingPreferences().RunAsync(services, context, logger);
                await new MetadataFieldMappingsMigration().RunAsync(services, context, logger);
                await new MigrateLinkPreferences().RunAsync(services, context, logger);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An exception occured while migrating the database. Mnema will not start");
                return;
            }

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Mnema uncounted an exceptions");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog((context, _, config) => { SerilogOptions.CreateConfig(context, config); })
            .ConfigureAppConfiguration((ctx, conf) =>
            {
                conf.Sources.Clear();

                var env = ctx.HostingEnvironment;
                conf.AddJsonFile("config/appsettings.json", true, false)
                    .AddJsonFile($"config/appsettings.{env.EnvironmentName}.json",
                        true, false)
                    .AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(builder => builder
                .UseKestrel(options => options
                    .ListenAnyIP(8080,
                        listenOptions => { listenOptions.Protocols = HttpProtocols.Http1; }))
                .UseStartup<Startup>());
    }

    /// <summary>
    /// Ensure NetVips does not cache
    /// </summary>
    /// <remarks>https://github.com/kleisauke/net-vips/issues/6#issuecomment-394379299</remarks>
    private static void InitNetVips()
    {
        Cache.Max = 0;
        Cache.MaxFiles = 0;
    }

    private static void PrintStartUp()
    {
        Console.WriteLine($"  {BuildInfo.AppName} v{BuildInfo.InformationalVersion}");
        Console.WriteLine($"  Runtime:  {BuildInfo.FrameworkDescription}");
        Console.WriteLine($"  OS:       {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
        Console.WriteLine();
    }
}
