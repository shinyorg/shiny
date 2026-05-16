using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Jobs;


public class ShinyJobWorker : ListenableWorker, CallbackToFutureAdapter.IResolver
{
    public const string ShinyCategoryIdentifier = nameof(ShinyCategoryIdentifier);
    readonly CancellationTokenSource cancelSource = new();
    public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams) { }


    public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
    {
        if (!Host.IsInitialized)
        {
            completer.SetException(new Java.Lang.Throwable("The Shiny Host is not initialized and cannot run jobs"));
        }
        else if (Host.GetService<IJobManager>() == null)
        {
            completer.SetException(new Java.Lang.Throwable("JobManager is not registered with Shiny"));
        }
        else
        {
            var host = Host.Current;
            var categoryId = this.InputData.GetString(ShinyCategoryIdentifier);
            var jobManager = (AbstractJobManager)host.Services.GetRequiredService<IJobManager>();
            var logger = host.Logging.CreateLogger<IJobManager>();

            if (categoryId.IsEmpty())
            {
                completer.Set(Result.InvokeSuccess());
            }
            else
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var jobs = jobManager.GetJobsByCategory(categoryId!);
                        foreach (var job in jobs)
                        {
                            await jobManager.RunJob(job, this.cancelSource.Token);
                        }
                        completer.Set(Result.InvokeSuccess());
                    }
                    catch (OperationCanceledException)
                    {
                        completer.SetCancelled();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in job worker category {Category}", categoryId);
                        completer.SetException(new Java.Lang.Throwable(ex.ToString()));
                    }
                });
            }
        }
        return completer;
    }


    public override IListenableFuture StartWork()
        => CallbackToFutureAdapter.GetFuture(this);


    public override void OnStopped()
    {
        this.cancelSource.Cancel();
        base.OnStopped();
    }
}
