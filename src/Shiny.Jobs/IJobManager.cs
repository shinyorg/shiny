using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Jobs;


public interface IJobManager
{
    /// <summary>
    /// Runs a one time, adhoc task - on iOS, it will initiate a background task
    /// </summary>
    void RunTask(string taskName, Func<CancellationToken, Task> task);

    /// <summary>
    /// This force runs the manager and any registered jobs
    /// </summary>
    Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false);
    
    /// <summary>
    /// Gets all registered jobs
    /// </summary>
    IReadOnlyDictionary<Type, JobRegistration> GetJobs();
}
