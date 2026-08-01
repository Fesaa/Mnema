using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Compression;
using System.Text.Json.Serialization.Metadata;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Common.Http;
using Mnema.Database.Extensions;
using Mnema.Metadata.Extensions;
using Mnema.Models;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Internal;
using Mnema.Providers.Extensions;
using Mnema.Server.Configuration;
using Mnema.Server.Extensions;
using Mnema.Server.Helpers;
using Mnema.Server.Middleware;
using Mnema.Services.Extensions;
using NeoSmart.Caching.Sqlite;
using Scalar.AspNetCore;
using Serilog;

namespace Mnema.Server;

public class Startup(IConfiguration configuration, IWebHostEnvironment env)
{

    private bool AuthDisabled => configuration.GetSection(AuthenticationExtensions.NoAuthentication).Get<bool>();

    public void ConfigureServices(IServiceCollection services)
    {
        var appConfig = configuration.GetSection("Application").Get<ApplicationConfiguration>();
        if (appConfig == null) throw new MnemaException("Application config must be set with key Application");

        services.AddSingleton(appConfig);

        services.AddTransient<AutomaticRateLimitRetryHandler>();
        services.AddTransient<ConnectResetRetryHandler>();
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddHttpMessageHandler<ConnectResetRetryHandler>();
            http.AddHttpMessageHandler<AutomaticRateLimitRetryHandler>();
        });

        services.AddProviders();
        services.AddMnemaServices(AuthDisabled);
        services.AddMetadataProviders(configuration, appConfig);

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
            options.JsonSerializerOptions.Converters.Add(new FormFieldDefinitionConverter());
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

        services.AddOpenApi();

        services.AddResponseCompression(opts =>
        {
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes;
            opts.EnableForHttps = true;
        });

        services.Configure<BrotliCompressionProviderOptions>(opts => { opts.Level = CompressionLevel.Fastest; });

        var redisConnectionString = configuration.GetConnectionString(ConfigurationKeys.RedisConnectionKey);
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
            services.AddSqliteCache(options =>
            {
                options.CachePath = Path.Join(appConfig.PersistentStorage, "Mnema.Cache.db");
            });
        }

        var autoMapperLicense = configuration.GetValue<string>("AutoMapperLicense");
        services.AddAutoMapper(cfg => cfg.LicenseKey = autoMapperLicense,
            typeof(AutoMapperProfiles).Assembly);

        services.AddMnemaDatabase(configuration);
        services.AddDatabaseServices();
        services.AddAndConfigureHangFire(configuration);
        services.AddAuthentication(configuration);
    }

    public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        if (AuthDisabled)
        {
            logger.LogCritical("Authentication has been disabled, Mnema is not secured. You must setup 3rd party authentication if or use OIDC");
        }

        app.UseResponseCompression();
        app.UseForwardedHeaders();
        app.UseRateLimiter();

        app.UseRouting();
        app.UseResponseCaching();

        if (env.IsDevelopment())
        {
            app.UseCors(opts =>
                opts.WithOrigins("http://localhost:4600")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
            );
        }
        else
        {
            app.UseCors();
        }

        app.UseOutputCache();
        app.UseSerilogRequestLogging(opts =>
        {
            opts.IncludeQueryInRequestPath = true;
        });
        app.UseMiddleware<ExceptionMiddleware>();

        app.UseStaticFiles(new StaticFileOptions
        {
            HttpsCompression = HttpsCompressionMode.Compress,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + TimeSpan.FromHours(24);
                ctx.Context.Response.Headers[Headers.RobotsTag] = "noindex,nofollow";
            }
        });
        app.UseDefaultFiles();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter()],
            FaviconPath = "favicon.ico",
            DefaultRecordsPerPage = 10
        });

        app.UseEndpoints(builder =>
            {
                builder.MapOpenApi();
                builder.MapScalarApiReference("/api-docs", async (options, ctx) =>
                {
                    if (!(ctx.User.Identity?.IsAuthenticated ?? false))
                    {
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await ctx.Response.CompleteAsync();
                        return;
                    }

                    options.Agent = new ScalarAgentOptions { Disabled = true };
                    options.Mcp = new ScalarMcpOptions { Disabled = true };
                    options.HideClientButton = true;
                    options.WithTitle("Mnema API Documentation");
                });

                builder.MapMnema();
                builder.MapControllers();
                builder.MapFallbackToController("Index", "Fallback");
            }
        );

        logger.LogInformation("Mnema starting up, stay tuned!");
    }
}
