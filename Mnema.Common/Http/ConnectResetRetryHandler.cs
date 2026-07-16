using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mnema.Common.Http;

public class ConnectResetRetryHandler(ILogger<ConnectResetRetryHandler> logger): DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            logger.LogWarning(ex, "Failed to send request. Retrying once after 1s");

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
