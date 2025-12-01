using System.IO.Compression;
using System.Reflection;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi;
using Mnema.Database.Extensions;
using Serilog;

namespace Mnema.Server;

public class Startup(IConfiguration configuration, IWebHostEnvironment env)
{
    public void ConfigureServices(IServiceCollection services)
    {

        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddRateLimiter();
        services.AddCors();
        services.AddSwaggerGen(c =>
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            c.UseInlineDefinitionsForEnums();
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "0.0.1",
                Title = "Mnema",
                Description = "Mnema is your self-hosted go-to solution for content downloading",
            });
        });
        
        services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes;
            opts.EnableForHttps = true;
        });

        services.Configure<BrotliCompressionProviderOptions>(opts =>
        {
            opts.Level = CompressionLevel.Fastest;
        });

        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Mnema";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddMnemaPostgresDatabase(configuration, env.IsDevelopment());
    }

    public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        app.UseResponseCompression();
        app.UseForwardedHeaders();
        app.UseRateLimiter();

        app.UseRouting();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseResponseCaching();
        app.UseCors(opts => 
            opts.WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader()
            );
        
        app.UseSerilogRequestLogging(opts =>
        {
            //opts.EnrichDiagnosticContext = LogEnricher.EnrichFromRequest;
            opts.IncludeQueryInRequestPath = true;
        });
        
        app.UseEndpoints(builder => 
            builder.MapControllers()
            );

        logger.LogInformation("Mnema starting up, stay tuned!");
    }
}