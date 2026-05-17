using System.Threading.Tasks;

namespace Shiny.Jobs;


/// <summary>
/// Convenience extensions over <see cref="IJobManager"/>.
/// </summary>
public static class JobExtensions
{
    /// <summary>
    /// Runs a specific registered job by identifier and returns a task that completes when the job finishes.
    /// </summary>
    /// <param name="jobManager">The job manager instance.</param>
    /// <param name="jobIdentifier">The identifier or full type name of the job to run.</param>
    public static Task RunJobAsTask(this IJobManager jobManager, string jobIdentifier)
    {
        if (jobManager is AbstractJobManager abstractManager)
            return abstractManager.RunJobAsTask(jobIdentifier);

        throw new System.InvalidOperationException("RunJobAsTask requires an AbstractJobManager implementation");
    }
}
