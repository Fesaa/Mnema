using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.Content;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.Content;

namespace Mnema.Services;

internal class SubscriptionService(
    ILogger<SubscriptionService> logger,
    IUnitOfWork unitOfWork,
    IServiceScopeFactory scopeFactory,
    ISettingsService settingsService
) : ISubscriptionService
{
    public async Task UpdateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto)
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
        sub.Status = dto.Status;

        unitOfWork.SubscriptionRepository.Update(sub);

        await unitOfWork.CommitAsync();
    }

    public async Task CreateSubscription(Guid userId, CreateOrUpdateSubscriptionDto dto)
    {
        var other = await unitOfWork.SubscriptionRepository.GetSubscriptionByContentId(dto.ContentId);
        if (other != null && other.UserId == userId)
            throw new MnemaException($"You already have a subscription on {dto.ContentId}");

        var sub = new Subscription
        {
            UserId = userId,
            Title = dto.Title,
            ContentId = dto.ContentId,
            BaseDir = dto.BaseDir,
            Metadata = dto.Metadata,
            Provider = dto.Provider,
            Status = dto.Status,
        };

        unitOfWork.SubscriptionRepository.Add(sub);

        await unitOfWork.CommitAsync();

        if (sub.Status == SubscriptionStatus.Enabled)
        {
            // Start subscription after subscribing
            await RunOnce(userId, sub.Id);
        }
    }

    public async Task RunOnce(Guid userId, Guid subId)
    {
        var sub = await unitOfWork.SubscriptionRepository.GetSubscription(subId);
        if (sub == null) throw new NotFoundException();

        if (sub.UserId != userId) throw new UnauthorizedAccessException();

        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredKeyedService<IContentManager>(sub.Provider);

        await manager.Download(sub.AsDownloadRequestDto());
    }

    public FormDefinition GetForm()
    {
        return new FormDefinition
        {
            Key = "edit-subscription-modal",
            Controls =
            [
                new FormControlDefinition
                {
                    Key = "title",
                    Field = "title",
                    Type = FormType.Text,
                    ForceSingle = true,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "content-id",
                    Field = "contentId",
                    Type = FormType.Text,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "base-dir",
                    Field = "baseDir",
                    Type = FormType.Directory,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                },
                new FormControlDefinition
                {
                    Key = "provider",
                    Field = "provider",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    Options = ISubscriptionService.SubscriptionProviders
                        .Select(provider => new FormControlOption(provider.ToString().ToLower(), provider))
                        .ToList(),
                },
                new FormControlDefinition()
                {
                    Key = "status",
                    Field = "status",
                    Type = FormType.DropDown,
                    ValueType = FormValueType.Integer,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .Build(),
                    DefaultOption = SubscriptionStatus.Enabled,
                    Options = Enum.GetValues<SubscriptionStatus>()
                        .Select(status => new FormControlOption(status.ToString().ToLower(), status))
                        .ToList(),
                }
            ]
        };
    }
}
