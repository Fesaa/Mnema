using System.IO.Compression;
using System.Reflection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Mnema.Common;
using Mnema.Database.Extensions;
using Mnema.Providers.Extensions;
using Mnema.Server.Extensions;
using Mnema.Server.Helpers;
using Mnema.Services.Extensions;
using Serilog;

namespace Mnema.Server;

public class Startup(IConfiguration configuration, IWebHostEnvironment env)
{
    public void ConfigureServices(IServiceCollection services)
    {

        services.AddProviders();
        services.AddMnemaServices();
        
        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new PaginationParamsModelBinderProvider());
        });
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
        services.AddIdentityServices(configuration, env);
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
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseStaticFiles(new StaticFileOptions
        {
            HttpsCompression = HttpsCompressionMode.Compress,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + TimeSpan.FromHours(24);
                ctx.Context.Response.Headers[Headers.RobotsTag] = "noindex,nofollow";
            },
        });
        app.UseDefaultFiles();
        
        app.UseEndpoints(builder =>
            {
                builder.MapControllers();
                builder.MapFallbackToController("Index", "Fallback");
            }
        );

        logger.LogInformation("Mnema starting up, stay tuned!");
    }
}