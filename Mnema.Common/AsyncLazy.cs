using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mnema.Common;

public sealed class AsyncLazy<T>(Func<Task<T>> factory)
{
    private readonly Lazy<Task<T>> _lazy = new(() => Task.Run(factory));

    public TaskAwaiter<T> GetAwaiter()
    {
        return _lazy.Value.GetAwaiter();
    }
}