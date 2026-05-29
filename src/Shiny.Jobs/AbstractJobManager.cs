using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shiny.Jobs;


/// <summary>
/// Platform-agnostic base implementation of <see cref="IJobManager"/> that handles
/// registration storage, batch execution, and logging.
/// </summary>
public abstract class AbstractJobManager(
    IServiceProvider container,
    ILogger logger,
    JobRegistrar registrar
) : IJobManager
{
    /// <summary>
    /// Logger available to derived implementations.
    /// </summary>
    protected ILogger Log => logger;

    /// <summary>
    /// The registrar that owns the set of registered jobs.
    /// </summary>
    protected JobRegistrar Registrar => registrar;

    /// <summary>
    /// Gets a value indicating whether a job batch is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Requests the platform permissions required to schedule background jobs.
    /// </summary>
    /// <returns>The resulting access state.</returns>
    public abstract Task<AccessState> RequestAccess();


    /// <inheritdoc />
    public virtual Task<JobRunResult> RunJob(Type jobType, bool runAsTask = false, CancellationToken cancellationToken = default)
    {
        if (!registrar.Jobs.TryGetValue(jobType, out var reg))
            throw new InvalidOperationException($"Job '{jobType.FullName}' is not registered");

        return runAsTask
            ? this.RunAsTask(jobType, reg, cancellationToken)
            : this.RunJob(jobType, reg, cancellationToken);
    }


    /// <summary>
    /// Runs a job under platform-specific extended-execution semantics.
    /// Override on a platform that supports a longer-lived background task
    /// (e.g. iOS <c>BeginBackgroundTask</c>, Android wake-lock); the default
    /// implementation simply forwards to <see cref="RunJob(Type, JobRegistration, CancellationToken)"/>.
    /// </summary>
    protected virtual Task<JobRunResult> RunAsTask(Type jobType, JobRegistration reg, CancellationToken cancellationToken)
        => this.RunJob(jobType, reg, cancellationToken);

    /// <inheritdoc />
    public virtual async void RunTask(string taskName, Func<CancellationToken, Task> task)
    {
        try
        {
            this.LogTask(JobState.Start, taskName);
            await task(CancellationToken.None).ConfigureAwait(false);
            this.LogTask(JobState.Finish, taskName);
        }
        catch (Exception ex)
        {
            this.LogTask(JobState.Error, taskName, ex);
        }
    }


    /// <inheritdoc />
    public IReadOnlyDictionary<Type, JobRegistration> GetJobs() => registrar.Jobs;


    /// <inheritdoc />
    public async Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default, bool runSequentially = false)
    {
        var list = new List<JobRunResult>();

        if (!this.IsRunning)
        {
            try
            {
                this.IsRunning = true;

                if (runSequentially)
                {
                    foreach (var (type, reg) in registrar.Jobs)
                    {
                        var result = await this
                            .RunJob(type, reg, cancelToken)
                            .ConfigureAwait(false);
                        list.Add(result);
                    }
                }
                else
                {
                    var tasks = registrar.Jobs
                        .Select(kvp => this.RunJob(kvp.Key, kvp.Value, cancelToken))
                        .ToList();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    list.AddRange(tasks.Select(x => x.Result));
                }
            }
            catch (Exception ex)
            {
                this.Log.LogError(ex, "Error running job batch");
            }
            finally
            {
                this.IsRunning = false;
            }
        }
        return list;
    }


    internal async Task<JobRunResult> RunJob(Type jobType, JobRegistration reg, CancellationToken cancelToken)
    {
        try
        {
            this.LogJob(JobState.Start, jobType, reg);
            var job = (IJob)container.GetRequiredService(jobType);
            await job.Run(cancelToken).ConfigureAwait(false);
            this.LogJob(JobState.Finish, jobType, reg);
            return new JobRunResult(reg, null);
        }
        catch (Exception ex)
        {
            this.LogJob(JobState.Error, jobType, reg, ex);
            return new JobRunResult(reg, ex);
        }
    }

    // Overload for callers that pass only JobRegistration (e.g. ShinyJobWorker)
    internal Task<JobRunResult> RunJob(JobRegistration reg, CancellationToken cancelToken)
        => this.RunJob(reg.JobType, reg, cancelToken);
    

    /// <summary>
    /// Gets all registrations matching a background task category identifier
    /// </summary>
    internal IList<JobRegistration> GetJobsByCategory(string categoryId)
    {
        return categoryId switch
        {
            "com.shiny.job" => registrar.Jobs.Values
                .Where(x => !x.DeviceCharging && x.RequiredInternetAccess == InternetAccess.None)
                .ToList(),

            "com.shiny.jobpower" => registrar.Jobs.Values
                .Where(x => x.DeviceCharging && x.RequiredInternetAccess == InternetAccess.None)
                .ToList(),

            "com.shiny.jobnet" => registrar.Jobs.Values
                .Where(x => !x.DeviceCharging && x.RequiredInternetAccess != InternetAccess.None)
                .ToList(),

            "com.shiny.jobpowernet" => registrar.Jobs.Values
                .Where(x => x.DeviceCharging && x.RequiredInternetAccess != InternetAccess.None)
                .ToList(),

            _ => new List<JobRegistration>()
        };
    }


    internal static string GetCategoryId(bool extPower, bool network)
    {
        var id = "com.shiny.job";
        if (extPower)
            id += "power";
        if (network)
            id += "net";
        return id;
    }


    /// <summary>
    /// Logs the lifecycle state of a job run. Override to customize logging.
    /// </summary>
    /// <param name="state">The current lifecycle state.</param>
    /// <param name="jobType">The CLR type of the job.</param>
    /// <param name="reg">The registration for the job.</param>
    /// <param name="exception">The exception, if any.</param>
    protected virtual void LogJob(JobState state, Type jobType, JobRegistration reg, Exception? exception = null)
    {
        if (exception == null)
            this.Log.LogInformation(state == JobState.Finish ? "Job '{JobName}' succeeded" : "Job '{JobName}' {State}", jobType.FullName, state);
        else
            this.Log.LogError(exception, "Error running job '{JobName}'", jobType.FullName);
    }


    /// <summary>
    /// Logs the lifecycle state of an ad-hoc task run. Override to customize logging.
    /// </summary>
    /// <param name="state">The current lifecycle state.</param>
    /// <param name="taskName">The task name.</param>
    /// <param name="exception">The exception, if any.</param>
    protected virtual void LogTask(JobState state, string taskName, Exception? exception = null)
    {
        if (exception == null)
            this.Log.LogInformation(state == JobState.Finish ? "Task '{TaskName}' succeeded" : "Task '{TaskName}' {State}", taskName, state);
        else
            this.Log.LogError(exception, "Task '{TaskName}' failed", taskName);
    }
}
