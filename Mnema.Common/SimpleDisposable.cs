using System;
using System.Threading;

namespace Mnema.Common;

public sealed class SimpleDisposable(Action onDisposed): IDisposable
{
    private int _disposed;
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            onDisposed.Invoke();
        }
    }
}
