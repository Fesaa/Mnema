using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Mnema.API;

namespace Mnema.Server.Helpers;

public class ExceptionJobFilter: JobFilterAttribute, IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is not FailedState failedState)
        {
            return;
        }

        var jobId = context.BackgroundJob.Id;
        var exception = failedState.Exception;

        BackgroundJob.Enqueue<IConnectionService>(s
            => s.CommunicateException($"Hangfire job {jobId} failed!", exception));
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {

    }
}
