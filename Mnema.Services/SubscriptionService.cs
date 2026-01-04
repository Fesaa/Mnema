using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

internal class SubscriptionService(
    ILogger<SubscriptionService> logger,
    IUnitOfWork unitOfWork,
    IServiceScopeFactory scopeFactory,
    ISettingsService settingsService
) : ISubscriptionService
{
    public async Task UpdateSubscription(Guid userId, SubscriptionDto dto)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscription(dto.Id);
        if (sub == null) throw new NotFoundException();

        if (sub.UserId != userId) throw new ForbiddenException();

        if (sub.Title != dto.Title) sub.Title = dto.Title;

        if (sub.BaseDir != dto.BaseDir)
        {
            if (dto.BaseDir.Contains("..")) throw new MnemaException("Invalid path");

            sub.BaseDir = dto.BaseDir;
        }

        sub.Provider = dto.Provider;
        sub.Metadata = dto.Metadata;
        sub.NoDownloadsRuns = 0;
        sub.RefreshFrequency = dto.RefreshFrequency;

        unitOfWork.SubscriptionRepository.Update(sub);

        await unitOfWork.CommitAsync();
    }

    public async Task CreateSubscription(Guid userId, SubscriptionDto dto)
    {
        var hour = await settingsService.GetSettingsAsync<int>(ServerSettingKey.SubscriptionRefreshHour);

        var sub = new Subscription
        {
            UserId = userId,
            Title = dto.Title,
            ContentId = dto.ContentId,
            BaseDir = dto.BaseDir,
            Metadata = dto.Metadata,
            Provider = dto.Provider,
            RefreshFrequency = dto.RefreshFrequency,
            LastRun = DateTime.MinValue,
            LastRunSuccess = true
        };

        sub.NextRun = sub.NextRunTime(hour);

        unitOfWork.SubscriptionRepository.Add(sub);

        await unitOfWork.CommitAsync();

        // Start subscription after subscribing
        await RunOnce(userId, sub.Id);
    }

    public async Task RunOnce(Guid userId, Guid subId)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscription(subId);
        if (sub == null) throw new NotFoundException();

        if (sub.UserId != userId) throw new UnauthorizedAccessException();

        var downloadRequest = new DownloadRequestDto
        {
            Provider = sub.Provider,
            Id = sub.ContentId,
            BaseDir = sub.BaseDir,
            TempTitle = sub.Title,
            DownloadMetadata = sub.Metadata,
            UserId = userId
        };

        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(sub.Provider);

        await manager.Download(downloadRequest);
    }
}