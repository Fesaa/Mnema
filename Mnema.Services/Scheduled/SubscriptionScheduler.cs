using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.User;

namespace Mnema.Services.Scheduled;

public class SubscriptionScheduler(ILogger<SubscriptionScheduler> logger, IServiceScopeFactory scopeFactory, IRecurringJobManagerV2 recurringJobManager): ISubscriptionScheduler
{

    private const string JobId = "subscriptions.daily";

    public async Task EnsureScheduledAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var refreshHour = await scope.ServiceProvider
            .GetRequiredService<ISettingsService>()
            .GetSettingsAsync<int>(ServerSettingKey.SubscriptionRefreshHour);

        await RescheduleAsync(refreshHour);
    }

    public async Task RescheduleAsync(int hour)
    {
        logger.LogDebug("Updating subscription task with hour {hour}", hour);
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        var subs = await unitOfWork.SubscriptionRepository.GetAllSubscriptions();
        foreach (var subscription in subs)
        {
            subscription.NextRun = subscription.NextRunTime(hour);
        }
        
        await unitOfWork.CommitAsync();
        
        Register(hour);
    }

    public async Task RunDaily()
    {
        using var scope = scopeFactory.CreateScope();
        
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var subHour = await scope.ServiceProvider
            .GetRequiredService<ISettingsService>()
            .GetSettingsAsync<int>(ServerSettingKey.SubscriptionRefreshHour);
            
        var subs = await unitOfWork.SubscriptionRepository.GetAllSubscriptions();

        var now = DateTime.Now;
        
        foreach (var subscription in subs)
        {
            var nextExec = subscription.NextRun.ToLocalTime();
            
            if (nextExec.Date != now.Date)
                continue;

            subscription.LastRun = DateTime.UtcNow;
            subscription.NextRun = subscription.NextRunTime(subHour);
            
            try
            {
                using var subScope = scopeFactory.CreateScope();

                var contentManager = subScope.ServiceProvider.GetRequiredKeyedService<IContentManager>(subscription.Provider);

                await contentManager.Download(new DownloadRequestDto
                {
                    Provider = subscription.Provider,
                    Id = subscription.ContentId,
                    BaseDir = subscription.BaseDir,
                    TempTitle = subscription.Title,
                    DownloadMetadata = subscription.Metadata,
                    UserId = subscription.UserId,
                    SubscriptionId = subscription.Id,
                });

                subscription.LastRunSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occured starting a subscription download");

                unitOfWork.NotificationRepository.AddNotification(new Notification
                {
                    Title = "Subscription failed to start",
                    UserId = subscription.UserId,
                    Summary = ex.Message,
                    Body = ex.StackTrace,
                    Colour = NotificationColour.Error,
                });

                subscription.LastRunSuccess = false;
            }
            finally
            {
                await unitOfWork.CommitAsync();
            }
        }
    }
    
    private void Register(int hour)
    {
        var cron = $"0 {hour} * * *";

        logger.LogDebug("Registering subscription task with cron {cron}", cron);
        recurringJobManager.AddOrUpdate<SubscriptionScheduler>(JobId, j => j.RunDaily(), cron,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Local,
            }
        );
    }
}