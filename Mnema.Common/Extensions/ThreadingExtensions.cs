using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mnema.Common.Extensions;

public static class ThreadingExtensions
{

    public static async Task DoWhile(
        this CancellationTokenSource tokenSource,
        ILogger logger,
        TimeSpan timeSpan,
        Func<Task> taskFactory,
        Func<Exception, Task<bool>> exceptionCatcher)
    {
        while (!tokenSource.IsCancellationRequested)
        {
            try
            {
                await taskFactory();

                await Task.Delay(timeSpan, tokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                /* Ignored */
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred in task loop");
                if (!await exceptionCatcher(ex))
                    return;
            }
        }
    }

}
