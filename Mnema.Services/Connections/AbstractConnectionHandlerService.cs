using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mnema.API;
using Mnema.Models.DTOs.Content;
using Mnema.Models.DTOs.UI;
using Mnema.Models.Entities;
using Mnema.Models.Entities.Content;

namespace Mnema.Services.Connections;

public abstract class AbstractConnectionHandlerService: IConnectionHandlerService
{
    public abstract List<ConnectionEvent> SupportedEvents { get; }
    public Task CommunicateDownloadStarted(Connection connection, DownloadInfo info)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateDownloadFinished(Connection connection, DownloadInfo info)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateDownloadFailure(Connection connection, DownloadInfo info, Exception ex)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateSubscriptionExhausted(Connection connection, DownloadInfo info)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateSeriesMonitored(Connection connection, MonitoredSeries series)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateSeriesUnmonitored(Connection connection, MonitoredSeries series)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateTooManyForAutomatedDownload(Connection connection, MonitoredSeries info, int amount)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateDownloadClientEvent(Connection connection, DownloadClient client)
    {
        throw new NotImplementedException();
    }

    public Task CommunicateException(Connection connection, string message, Exception ex)
    {
        throw new NotImplementedException();
    }

    public abstract Task<List<FormControlDefinition>> GetConfigurationFormControls(CancellationToken cancellationToken);
}
