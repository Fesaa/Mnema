using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Database.Repositories;

namespace Mnema.Database;

public class UnitOfWork(ILogger<UnitOfWork> logger, MnemaDataContext ctx, IMapper mapper) : IUnitOfWork
{
    public ISubscriptionRepository SubscriptionRepository { get; } = new SubscriptionRepository(ctx, mapper);
    public IPagesRepository PagesRepository { get; } = new PagesRepository(ctx, mapper);
    public IUserRepository UserRepository { get; } = new UserRepository(ctx, mapper);
    public ISettingsRepository SettingsRepository { get; } = new SettingsRepository(ctx, mapper);
    public INotificationRepository NotificationRepository { get; } = new NotificationRepository(ctx, mapper);
    public IConnectionRepository ConnectionRepository { get; } = new ConnectionRepository(ctx, mapper);
    public IContentReleaseRepository ContentReleaseRepository { get; } = new ContentReleaseRepository(ctx, mapper);
    public IDownloadClientRepository DownloadClientRepository { get; } = new DownloadClientRepository(ctx, mapper);
    public IContentReleaseRepository ImportedReleaseRepository { get; } = new ImportedContentReleaseRepository(ctx, mapper);
    public IMonitoredSeriesRepository MonitoredSeriesRepository { get; } = new MonitoredSeriesRepository(ctx, mapper);

    public async Task<bool> CommitAsync()
    {
        return await ctx.SaveChangesAsync() > 0;
    }

    public bool HasChanges()
    {
        return ctx.ChangeTracker.HasChanges();
    }

    public async Task<bool> RollbackAsync()
    {
        try
        {
            await ctx.Database.RollbackTransactionAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured during rollback");
        }

        return true;
    }
}
