using System.Threading.Tasks;

namespace Shiny.Jobs;


public static class JobExtensions
{
    /// <summary>
    /// This will run a job from an event/delegate - the same rules as the job running normally still apply
    /// You don't need to use this if you are using foreground jobs (ie. services.UseJobForegroundService)
    /// </summary>
    /// <param name="jobManager"></param>
    /// <param name="jobIdentifier"></param>
    /// <returns></returns>
    public static Task RunJobAsTask(this IJobManager jobManager, string jobIdentifier)
    {
        var tcs = new TaskCompletionSource<object?>();
        jobManager.RunTask(jobIdentifier, async ct =>
        {
            var result = await jobManager
                .Run(jobIdentifier, ct)
                .ConfigureAwait(false);

            if (result.Success)
                tcs.SetResult(null);
            else
                tcs.SetResult(result.Exception);
        });
        return tcs.Task;
    }
}
