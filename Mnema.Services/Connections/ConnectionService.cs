using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;

namespace Mnema.Services.Connections;

internal class ConnectionService(
    ILogger<ConnectionService> logger,
    IServiceScopeFactory scopeFactory,
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider
) : IConnectionService
{
    public void CommunicateDownloadStarted(DownloadInfo info)
    {
        DoForAll(ConnectionEvent.DownloadStarted, (service, connection)
            => service.CommunicateDownloadStarted(connection, info));
    }

    public void CommunicateDownloadFinished(DownloadInfo info)
    {
        DoForAll(ConnectionEvent.DownloadFinished, (service, connection)
            => service.CommunicateDownloadFinished(connection, info));
    }

    public void CommunicateDownloadFailure(DownloadInfo info, Exception ex)
    {
        DoForAll(ConnectionEvent.DownloadFailure, (service, connection)
            => service.CommunicateDownloadFailure(connection, info, ex));
    }

    public void CommunicateSeriesExhausted(DownloadInfo info)
    {
        DoForAll(ConnectionEvent.SubscriptionExhausted, (service, connection)
            => service.CommunicateSubscriptionExhausted(connection, info));
    }

    public async Task UpdateConnection(ConnectionDto dto, CancellationToken cancellationToken)
    {
        var connection = await unitOfWork.ConnectionRepository.GetById(dto.Id, cancellationToken) ??
                         new Connection { Type = dto.Type, Metadata = new MetadataBag() };

        connection.Name = dto.Name;
        connection.FollowedEvents = dto.FollowedEvents;

        var service = serviceProvider.GetKeyedService<IConnectionHandlerService>(connection.Type);
        if (service == null)
        {
            logger.LogWarning(
                "Could not find external connection service for {ExternalConnectionType}, while one was configured",
                connection.Type.ToString());
            throw new NotFoundException();
        }

        var controls = await service.GetConfigurationFormControls(cancellationToken);
        foreach (var control in controls)
            connection.Metadata[control.Key] = dto.Metadata.GetStrings(control.Key).ToList();

        if (connection.Id.Equals(Guid.Empty))
            unitOfWork.ConnectionRepository.Add(connection);
        else
            unitOfWork.ConnectionRepository.Update(connection);

        await unitOfWork.CommitAsync();
    }

    public async Task<FormDefinition> GetForm(ConnectionType type, CancellationToken cancellationToken)
    {
        var service = serviceProvider.GetKeyedService<IConnectionHandlerService>(type);
        if (service == null)
        {
            logger.LogWarning(
                "Could not find external connection service {ExternalConnectionType}, while one was configured",
                type.ToString());
            throw new NotFoundException();
        }

        var controls = await service.GetConfigurationFormControls(cancellationToken);

        return new FormDefinition
        {
            Key = $"settings.connections.{type}",
            Controls =
            [
                new FormControlDefinition
                {
                    Key = "name",
                    Field = "name",
                    Type = FormType.Text,
                    Validators = new FormValidatorsBuilder()
                        .WithRequired()
                        .WithMinLength(1)
                        .Build()
                },
                new FormControlDefinition
                {
                    Key = "followed-events",
                    Field = "followedEvents",
                    Type = FormType.MultiSelect,
                    ValueType = FormValueType.Integer,
                    Options = service.SupportedEvents
                        .Select(@event => new FormControlOption(@event.ToString(), @event))
                        .ToList()
                },
                ..controls
            ]
        };
    }

    private void DoForAll(ConnectionEvent @event,
        Func<IConnectionHandlerService, Connection, Task> consumer)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var connections = await scopedUnitOfWork.ConnectionRepository
                    .GetAll(CancellationToken.None);

                List<Task> tasks = [];

                foreach (var connection in connections.Where(c => c.FollowedEvents.Contains(@event)))
                {
                    var service =
                        scope.ServiceProvider.GetKeyedService<IConnectionHandlerService>(connection.Type);
                    if (service == null)
                    {
                        logger.LogWarning(
                            "Could not find external connection service {ExternalConnectionType}, while one was configured",
                            connection.Type.ToString());
                        continue;
                    }

                    if (!service.SupportedEvents.Contains(@event)) continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await consumer(service, connection);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed communicating with external connection {Type} for {Event}",
                                connection.Type.ToString(), @event.ToString());
                        }
                    }));
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An exception occured while communication with an external connection");
            }
        });
    }
}
