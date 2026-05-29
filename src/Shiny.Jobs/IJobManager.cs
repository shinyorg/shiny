using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


/// <summary>
/// Coordinates registered background jobs and ad-hoc tasks on the host platform.
/// </summary>
public interface IJobManager
{
    /// <summary>
    /// Runs a one-time, ad-hoc task. On iOS this initiates a background task to extend execution time.
    /// </summary>
    /// <param name="taskName">A human-readable name used for logging and platform task identification.</param>
    /// <param name="task">The work to execute.</param>
    void RunTask(string taskName, Func<CancellationToken, Task> task);

    /// <summary>
    /// Runs a job on a background task
    /// </summary>
    /// <param name="jobIdentifier"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<JobRunResult> RunJobAsTask(string jobIdentifier, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Force-runs all registered jobs and returns the result for each.
    /// </summary>
    /// <param name="cancelToken">Token used to cancel running jobs.</param>
    /// <param name="runSequentially">When true, jobs run one after another. When false, jobs run concurrently.</param>
    /// <returns>The result of each executed job.</returns>
    Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false);

    /// <summary>
    /// Gets all currently registered jobs keyed by their CLR type.
    /// </summary>
    IReadOnlyDictionary<Type, JobRegistration> GetJobs();
}
