using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.External;
using Mnema.Common;
using Mnema.Common.Exceptions;
using Mnema.Models.DTOs;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities.External;
using ValueType = Mnema.Models.DTOs.UI.ValueType;

namespace Mnema.Services.External;

internal class ExternalConnectionService(
    ILogger<ExternalConnectionService> logger,
    IServiceScopeFactory scopeFactory,
    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider
) : IExternalConnectionService
{
    public void CommunicateDownloadStarted(DownloadInfo info)
    {
        DoForAll(ExternalConnectionEvent.DownloadStarted, (service, connection)
            => service.CommunicateDownloadStarted(connection, info));
    }

    public void CommunicateDownloadFinished(DownloadInfo info)
    {
        DoForAll(ExternalConnectionEvent.DownloadFinished, (service, connection)
            => service.CommunicateDownloadFinished(connection, info));
    }

    public void CommunicateDownloadFailure(DownloadInfo info, Exception ex)
    {
        DoForAll(ExternalConnectionEvent.DownloadFailure, (service, connection)
            => service.CommunicateDownloadFailure(connection, info, ex));
    }

    public async Task UpdateConnection(ExternalConnectionDto dto, CancellationToken cancellationToken)
    {
        var connection = await unitOfWork.ExternalConnectionRepository.GetConnectionById(dto.Id, cancellationToken) ??
                         new ExternalConnection { Type = dto.Type, Metadata = new MetadataBag() };

        connection.Name = dto.Name;
        connection.FollowedEvents = dto.FollowedEvents;

        var service = serviceProvider.GetKeyedService<IExternalConnectionHandlerService>(connection.Type);
        if (service == null)
        {
            logger.LogWarning(
                "Could not find external connection service {ExternalConnectionType}, while one was configured",
                connection.Type.ToString());
            throw new NotFoundException();
        }

        var controls = await service.GetConfigurationFormControls(cancellationToken);
        foreach (var control in controls)
            connection.Metadata[control.Key] = dto.Metadata.GetStrings(control.Key).ToList();

        if (connection.Id.Equals(Guid.Empty))
            unitOfWork.ExternalConnectionRepository.Add(connection);
        else
            unitOfWork.ExternalConnectionRepository.Update(connection);

        await unitOfWork.CommitAsync();
    }

    public async Task<FormDefinition> GetForm(ExternalConnectionType type, CancellationToken cancellationToken)
    {
        var service = serviceProvider.GetKeyedService<IExternalConnectionHandlerService>(type);
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
            Key = $"settings.external-connections.{type}",
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
                    ValueType = ValueType.Integer,
                    Options = service.SupportedEvents
                        .Select(@event => new FormControlOption(@event.ToString(), @event))
                        .ToList()
                },
                ..controls
            ]
        };
    }

    private void DoForAll(ExternalConnectionEvent @event,
        Func<IExternalConnectionHandlerService, ExternalConnection, Task> consumer)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var connections = await scopedUnitOfWork.ExternalConnectionRepository
                    .GetAllConnections(CancellationToken.None);

                List<Task> tasks = [];

                foreach (var connection in connections.Where(c => c.FollowedEvents.Contains(@event)))
                {
                    var service =
                        scope.ServiceProvider.GetKeyedService<IExternalConnectionHandlerService>(connection.Type);
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
                            logger.LogError(ex, "Failed communicating with external connection {Type}",
                                connection.Type.ToString());
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
