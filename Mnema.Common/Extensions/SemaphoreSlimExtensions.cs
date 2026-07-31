using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mnema.Common.Extensions;

public static class SemaphoreSlimExtensions
{

    public static async Task<IDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        await semaphore.WaitAsync(cancellationToken);
        return new SimpleDisposable(() => semaphore.Release());
    }

}
