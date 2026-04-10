using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Mnema.API;
using Mnema.Common.Extensions;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.DTOs.User;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;
using Mnema.Models.Entities.User;

namespace Mnema.Services.Connections;

internal class NativeConnectionService(
    IUnitOfWork unitOfWork,
    IMessageService messageService,
    IMapper mapper
) : AbstractConnectionHandlerService
{
    private const int MaxSummaryLength = 512;
    private const int MaxBodyLength = 4096;

    public override List<ConnectionEvent> SupportedEvents { get; } =
    [
        ConnectionEvent.DownloadStarted,
        ConnectionEvent.DownloadFinished,
        ConnectionEvent.DownloadFailure,
        ConnectionEvent.SubscriptionExhausted,
        ConnectionEvent.TooManyForAutomatedDownload,
        ConnectionEvent.DownloadClientEvents
    ];

    public override Task CommunicateDownloadStarted(Connection connection, DownloadInfo info)
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

    public override Task CommunicateDownloadFinished(Connection connection, DownloadInfo info)
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

    public override Task CommunicateDownloadFailure(Connection connection, DownloadInfo info, Exception ex)
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

    public override Task CommunicateSubscriptionExhausted(Connection connection, DownloadInfo info)
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

    public override Task CommunicateTooManyForAutomatedDownload(Connection connection, MonitoredSeries info, int amount)
    {
        return SendNotification(new Notification()
        {
            Title = "Manual intervention required",
            Summary = $"Cannot automatically start download for {info.Title} as it wants to download {amount} chapters at once.",
            Body = string.Empty,
            Colour = NotificationColour.Warning,
            UserId = info.UserId
        });
    }

    public new async Task CommunicateDownloadClientEvent(Connection connection, DownloadClient client)
    {
        var users = await unitOfWork.UserRepository.GetUsers();

        foreach (var user in users)
        {
            await SendNotification(new Notification
            {
                Title = client.IsFailed ? "Download client locked" : "Download client unlocked",
                Summary = client.IsFailed
                    ? $"Client {client.Name} is unreachable and is locked until {client.FailedAt?.AddHours(1)}"
                    : $"Client {client.Name} is reachable again and has been unlocked",
                Body = string.Empty,
                Colour = client.IsFailed ? NotificationColour.Warning : NotificationColour.Primary,
                UserId = user.Id,
            });
        }
    }

    public override Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken)
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
