using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Jobs
{
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
        event EventHandler<JobInfo> JobStarted;


        /// <summary>
        /// Fires as each job finishes
        /// </summary>
        event EventHandler<JobRunResult> JobFinished;


        /// <summary>
        /// Requests/ensures appropriate platform permissions where necessary
        /// </summary>
        /// <returns></returns>
        Task<AccessState> RequestAccess();


        /// <summary>
        /// This force runs the manager and any registered jobs
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default);


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
        Task<JobInfo> GetJob(string jobIdentifier);


        /// <summary>
        /// Gets current registered jobs
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<JobInfo>> GetJobs();


        /// <summary>
        /// Create a new job
        /// </summary>
        /// <param name="jobInfo"></param>
        Task Schedule(JobInfo jobInfo);


        /// <summary>
        /// Cancel a job
        /// </summary>
        /// <param name="jobName"></param>
        Task Cancel(string jobName);


        /// <summary>
        /// Cancel All Jobs
        /// </summary>
        Task CancelAll();
    }
}
