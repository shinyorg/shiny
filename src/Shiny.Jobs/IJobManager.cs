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
    /// <param name="taskName"></param>
    /// <param name="task"></param>
    void RunTask(string taskName, Func<CancellationToken, Task> task);


    /// <summary>
    /// Flag to see if job manager is running registered tasks
    /// </summary>
    bool IsRunning { get; }


    /// <summary>
    /// Fires just as a job is about to start
    /// </summary>
    IObservable<JobInfo> JobStarted { get; }


    /// <summary>
    /// Fires as each job finishes
    /// </summary>
    IObservable<JobRunResult> JobFinished { get; }


    /// <summary>
    /// Requests/ensures appropriate platform permissions where necessary
    /// </summary>
    /// <returns></returns>
    Task<AccessState> RequestAccess();


    /// <summary>
    /// This force runs the manager and any registered jobs
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <param name="runSequentially"></param>
    /// <returns></returns>
    Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false);


    /// <summary>
    /// Run a specific job adhoc
    /// </summary>
    /// <param name="jobIdentifier"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default);


    /// <summary>
    /// Get a job by its registered name
    /// </summary>
    /// <param name="jobIdentifier"></param>
    /// <returns></returns>
    JobInfo? GetJob(string jobIdentifier);


    /// <summary>
    /// Gets current registered jobs
    /// </summary>
    /// <returns></returns>
    IList<JobInfo> GetJobs();


    /// <summary>
    /// Create a new job
    /// </summary>
    /// <param name="jobInfo"></param>
    void Register(JobInfo jobInfo);


    /// <summary>
    /// Cancel a job
    /// </summary>
    /// <param name="jobName"></param>
    void Cancel(string jobName);


    /// <summary>
    /// Cancel All Jobs
    /// </summary>
    void CancelAll();
}
