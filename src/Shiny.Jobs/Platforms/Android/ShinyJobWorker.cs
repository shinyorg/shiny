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
    public const string ShinyJobIdentifier = nameof(ShinyJobIdentifier);
    readonly CancellationTokenSource cancelSource = new();
    public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams) {}


    public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
    {
        var jobName = this.InputData.GetString(ShinyJobIdentifier);
        var jobManager = Host.Current.Services.GetRequiredService<IJobManager>();

        //if (jobManager != null && !jobName.IsEmpty())
        if (jobName.IsEmpty() || jobManager == null)
        {
            //completer.Set(Result.InvokeFailure());
            completer.Set(Result.InvokeSuccess());
        }
        else
        {
            jobManager
                .Run(jobName, this.cancelSource.Token)
                .ContinueWith(x =>
                {
                    switch (x.Status)
                    {
                        case TaskStatus.Canceled:
                            completer.SetCancelled();
                            break;

                        case TaskStatus.Faulted:
                            Host
                                .Current
                                .Logging
                                .CreateLogger<IJobManager>()
                                .LogError(x.Exception, "Error in job");

                            completer.SetException(new Java.Lang.Throwable(x.Exception.ToString()));
                            break;

                        case TaskStatus.RanToCompletion:
                            completer.Set(Result.InvokeSuccess());
                            break;
                    }
                });
        }
        return "AsyncOp";
    }


    public override IListenableFuture StartWork()
        => CallbackToFutureAdapter.GetFuture(this);


    public override void OnStopped()
    {
        this.cancelSource.Cancel();
        base.OnStopped();
    }
}
