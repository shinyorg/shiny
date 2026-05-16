using System.Threading.Tasks;

namespace Shiny.Jobs;


public static class JobExtensions
{
    /// <summary>
    /// Runs a specific registered job by identifier and returns a task that completes when the job finishes.
    /// </summary>
    public static Task RunJobAsTask(this IJobManager jobManager, string jobIdentifier)
    {
        if (jobManager is AbstractJobManager abstractManager)
            return abstractManager.RunJobAsTask(jobIdentifier);

        throw new System.InvalidOperationException("RunJobAsTask requires an AbstractJobManager implementation");
    }
}
