using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using Hangfire;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Database.Extensions;
using Mnema.Models;
using Mnema.Models.Internal;
using Mnema.Providers.Extensions;
using Mnema.Server.Configuration;
using Mnema.Server.Extensions;
using Mnema.Server.Helpers;
using Mnema.Server.Middleware;
using Mnema.Services.Extensions;
using Serilog;

namespace Mnema.Server;

public class Startup(IConfiguration configuration, IWebHostEnvironment env)
{
    public void ConfigureServices(IServiceCollection services)
    {

        var appConfig = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (appConfig == null)
        {
            throw new MnemaException($"Application config must be set with key Application");
        }
        
        services.AddSingleton(appConfig);

        services.AddProviders();
        services.AddMnemaServices();

        services.AddScoped<IFileSystem, FileSystem>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddHostedService<JobsBootstrapper>();

        services.AddSignalR();
        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new PaginationParamsModelBinderProvider());

            options.CacheProfiles
                .AddCacheProfile(CacheProfiles.FiveMinutes, TimeSpan.FromMinutes(5))
                .AddCacheProfile(CacheProfiles.OneHour, TimeSpan.FromHours(1))
                .AddCacheProfile(CacheProfiles.OneDay, TimeSpan.FromDays(1))
                .AddCacheProfile(CacheProfiles.OneWeek, TimeSpan.FromDays(7));
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new EmptyStringToGuidConverter());
        });
        services.AddEndpointsApiExplorer();
        services.AddRateLimiter();
        services.AddCors();
        services.AddOutputCache(options =>
        {
            options
                .AddCachePolicy(CacheProfiles.FiveMinutes, TimeSpan.FromMinutes(5))
                .AddCachePolicy(CacheProfiles.OneHour, TimeSpan.FromHours(1))
                .AddCachePolicy(CacheProfiles.OneDay, TimeSpan.FromDays(1))
                .AddCachePolicy(CacheProfiles.OneWeek, TimeSpan.FromDays(7));
        });
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
            services.AddStackExchangeRedisOutputCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "Mnema/output-cache";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        var autoMapperLicense = configuration.GetValue<string>("AutoMapperLicense");
        services.AddAutoMapper(cfg => cfg.LicenseKey = autoMapperLicense,
            typeof(AutoMapperProfiles).Assembly);

        services.AddMnemaPostgresDatabase(configuration, env.IsDevelopment());
        services.AddDatabaseServices();
        services.AddAndConfigureHangFire(configuration);
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
            opts.WithOrigins("http://localhost:4600")
                .AllowAnyMethod()
                .AllowAnyHeader()
            );
        app.UseOutputCache();
        app.UseSerilogRequestLogging(opts =>
        {
            //opts.EnrichDiagnosticContext = LogEnricher.EnrichFromRequest;
            opts.IncludeQueryInRequestPath = true;
        });
        app.UseMiddleware<ExceptionMiddleware>();
        
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()],
            FaviconPath = "favicon.ico",
            DefaultRecordsPerPage = 10,
        });

        app.UseStaticFiles(new StaticFileOptions
        {
            HttpsCompression = HttpsCompressionMode.Compress,
            OnPrepareResponse = ctx =>
            {
                if (ctx.Context.User.Identity?.IsAuthenticated ?? false)
                {
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + TimeSpan.FromHours(24);
                    ctx.Context.Response.Headers[Headers.RobotsTag] = "noindex,nofollow";
                }
                else
                {
                    ctx.Context.Response.Redirect($"/Auth/login?returnUrl={Uri.EscapeDataString(ctx.Context.Request.Path)}");
                }
            },
        });
        app.UseDefaultFiles();
        
        app.UseEndpoints(builder =>
            {
                builder.MapMnema();
                builder.MapControllers();
                builder.MapFallbackToController("Index", "Fallback");
            }
        );

        logger.LogInformation("Mnema starting up, stay tuned!");
    }
}