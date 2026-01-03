
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
using Mnema.API;
using Mnema.Database;
using Mnema.Server.Logging;
using Serilog;
using Serilog.Templates;

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

                await context.SeedDatabase();
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
            Log.Fatal(ex, "Mnema failed to startup");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
            .UseSerilog((context, _, config) =>
            {
                SerilogOptions.CreateConfig(context, config);
            })
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
                    .ListenAnyIP(8080, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                    }))
                .UseStartup<Startup>());

}