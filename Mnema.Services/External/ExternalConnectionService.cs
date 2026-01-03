using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mnema.API;
using Mnema.API.External;
using Mnema.Models.DTOs.Content;
using Mnema.Models.Entities.External;

namespace Mnema.Services.External;

public class ExternalConnectionService(
    ILogger<ExternalConnectionService> logger,
    IServiceScopeFactory scopeFactory
    ): IExternalConnectionService
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

    private void DoForAll(ExternalConnectionEvent @event, Func<IExternalConnectionHandlerService, ExternalConnection, Task> consumer)
    {
        Task.Run(async () =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var connections = await unitOfWork.ExternalConnectionRepository
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