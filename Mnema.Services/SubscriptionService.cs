using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

public class SubscriptionService(ILogger<SubscriptionService> logger, IUnitOfWork unitOfWork): ISubscriptionService
{
    
    public async Task UpdateSubscription(Guid userId, SubscriptionDto dto)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscription(dto.Id);
        if (sub == null) throw new NotFoundException();

        if (sub.UserId != userId) throw new ForbiddenException();

        if (sub.BaseDir != dto.BaseDir)
        {
            if (dto.BaseDir.Contains("..")) throw new MnemaException("Invalid path");

            sub.BaseDir = dto.BaseDir;
        }

        sub.Metadata = dto.Metadata;
        sub.NoDownloadsRuns = 0;
        if (!string.IsNullOrEmpty(dto.LastDownloadDir))
            sub.LastDownloadDir = dto.LastDownloadDir;
        
        unitOfWork.SubscriptionRepository.Update(sub);

        await unitOfWork.CommitAsync();
    }
    public async Task CreateSubscription(Guid userId, SubscriptionDto dto)
    {
        var sub = new Subscription
        {
            UserId = userId,
            ContentId = dto.ContentId,
            BaseDir = dto.BaseDir,
            Metadata = dto.Metadata,
        };
        
        unitOfWork.SubscriptionRepository.Add(sub);

        await unitOfWork.CommitAsync();
    }
}