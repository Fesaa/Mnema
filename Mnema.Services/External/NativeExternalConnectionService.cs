using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Mnema.API;
using Mnema.API.External;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities.External;
using Mnema.Models.Entities.User;

namespace Mnema.Services.External;

internal class NativeExternalConnectionService(
    IUnitOfWork unitOfWork,
    IMessageService messageService,
    IMapper mapper
) : IExternalConnectionHandlerService
{
    private const int MaxSummaryLength = 512;
    private const int MaxBodyLength = 4096;

    public List<ExternalConnectionEvent> SupportedEvents { get; } =
    [
        ExternalConnectionEvent.DownloadStarted,
        ExternalConnectionEvent.DownloadFinished,
        ExternalConnectionEvent.DownloadFailure,
        ExternalConnectionEvent.SubscriptionExhausted
    ];

    public Task CommunicateDownloadStarted(ExternalConnection connection, DownloadInfo info)
    {
        return SendNotification(new Notification
        {
            Title = "Download Started",
            Summary = $"Download for {info.Name} has started.",
            Body = info.Description,
            Colour = NotificationColour.Primary,
            UserId = info.UserId
        });
    }

    public Task CommunicateDownloadFinished(ExternalConnection connection, DownloadInfo info)
    {
        return SendNotification(new Notification
        {
            Title = "Download Finished",
            Summary = $"Download for {info.Name} has finished.",
            Body = info.Description,
            Colour = NotificationColour.Primary,
            UserId = info.UserId
        });
    }

    public Task CommunicateDownloadFailure(ExternalConnection connection, DownloadInfo info, Exception ex)
    {
        return SendNotification(new Notification
        {
            Title = "Download Failed",
            Summary = $"Download for {info.Name} has failed.",
            Body = ex.Message.Limit(MaxBodyLength),
            Colour = NotificationColour.Error,
            UserId = info.UserId
        });
    }

    public Task CommunicateSubscriptionExhausted(ExternalConnection connection, DownloadInfo info)
    {
        return SendNotification(new Notification
        {
            Title = "Subscription Completed",
            Summary = $"The subscription for {info.Name} has downloaded everything.",
            Body = info.Description,
            Colour = NotificationColour.Primary,
            UserId = info.UserId
        });
    }

    public Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
    {
        return Task.FromResult(new List<FormControlDefinition>());
    }

    private async Task SendNotification(Notification notification)
    {
        unitOfWork.NotificationRepository.AddNotification(notification);
        await unitOfWork.CommitAsync();

        await messageService.NotificationAdded(notification.UserId, 1);

        var dto = mapper.Map<NotificationDto>(notification);
        await messageService.Notify(notification.UserId, dto);
    }
}
