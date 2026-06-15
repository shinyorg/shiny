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
    /// Runs a single registered job by its CLR type.
    /// </summary>
    /// <param name="jobType">The registered <see cref="IJob"/> implementation type.</param>
    /// <param name="cancellationToken">Token used to cancel the running job.</param>
    /// <returns>The job's execution result.</returns>
    Task<JobRunResult> RunJob(Type jobType, CancellationToken cancellationToken = default);
    
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
