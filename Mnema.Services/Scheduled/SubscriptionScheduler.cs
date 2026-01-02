using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Services.Scheduled;

public class SubscriptionScheduler(ILogger<SubscriptionScheduler> logger, IServiceScopeFactory scopeFactory, IRecurringJobManagerV2 recurringJobManager): ISubscriptionScheduler
{

    private const string JobId = "subscriptions.daily";
    private const string WatcherJobId = "subscriptions.rss";

    private static readonly RecurringJobOptions RecurringJobOptions = new()
    {
        TimeZone = TimeZoneInfo.Local,
    };

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

                await contentManager.Download(subscription.AsDownloadRequestDto());

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

    public async Task RunWatcher()
    {
        using var scope = scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var downloadService = scope.ServiceProvider.GetRequiredService<IDownloadService>();

        var subscriptions = await unitOfWork.SubscriptionRepository.GetAllSubscriptions();
        if (subscriptions.Count == 0)
            return;

        var subsById = subscriptions
            .GroupBy(s => s.ContentId)
            .ToDictionary(g => g.Key, g => g.First());

        var providers = subscriptions
            .Select(s => s.Provider)
            .Distinct()
            .ToList();

        logger.LogTrace("Searching for recent updated for {Providers} providers", providers.Count);

        var (total, subscriptionsToStart) = await ProcessRecentlyUpdated(scope, providers, subsById);

        logger.LogInformation(
            "Found {Reports} updated reports, starting download for {ToStart}",
            total,
            subscriptionsToStart.Count
        );

        foreach (var subscription in subscriptionsToStart)
        {
            await downloadService.StartDownload(subscription.AsDownloadRequestDto());
        }
    }

    private async Task<(int, List<Subscription>)> ProcessRecentlyUpdated(
        IServiceScope scope, List<Provider> providers, Dictionary<string, Subscription> subsById
        )
    {
        var totalUpdated = 0;
        var subscriptionsToStart = new List<Subscription>();

        foreach (var provider in providers)
        {
            var repository = scope.ServiceProvider.GetKeyedService<IRepository>(provider);
            if (repository == null)
            {
                logger.LogWarning("Repository for {Provider} not found, while a subscription exists", provider.ToString());
                continue;
            }

            var recentlyUpdated = await repository.GetRecentlyUpdated(CancellationToken.None);
            totalUpdated += recentlyUpdated.Count;

            var matching = recentlyUpdated
                .Where(id => subsById.TryGetValue(id, out _))
                .Select(id => subsById[id])
                .ToList();

            if (matching.Count == 0)
                continue;

            logger.LogDebug(
                "Found {Amount} subscriptions that have updated for provider {Provider}",
                matching.Count, provider
            );

            subscriptionsToStart.AddRange(matching);
        }

        return (totalUpdated, subscriptionsToStart);
    }

    
    private void Register(int hour)
    {
        var cron = $"0 {hour} * * *";

        logger.LogDebug("Registering subscription task with cron {cron}", cron);
        recurringJobManager.AddOrUpdate<SubscriptionScheduler>(JobId, j => j.RunDaily(),
            cron, RecurringJobOptions);

        cron = "*/15 * * * *";

        logger.LogDebug("Registering subscription watcher task with cron {cron}", cron);
        recurringJobManager.AddOrUpdate<SubscriptionScheduler>(WatcherJobId, j => j.RunWatcher(),
            cron, RecurringJobOptions);
    }
}