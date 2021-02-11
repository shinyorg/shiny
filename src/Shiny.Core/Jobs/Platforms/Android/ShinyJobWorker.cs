using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;

using Microsoft.Extensions.Logging;

namespace Shiny.Jobs
{
    public class ShinyJobWorker : ListenableWorker, CallbackToFutureAdapter.IResolver
    {
        public const string ShinyJobIdentifier = nameof(ShinyJobIdentifier);
        readonly CancellationTokenSource cancelSource = new CancellationTokenSource();


        public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }


        public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
        {
            var jobName = this.InputData.GetString(ShinyJobIdentifier);
            if (jobName.IsEmpty())
            {
                completer.Set(Result.InvokeFailure());
            }
            else
            {
                ShinyHost
                    .Resolve<IJobManager>()
                    .Run(jobName, this.cancelSource.Token)
                    .ContinueWith(x =>
                    {
                        switch (x.Status)
                        {
                            case TaskStatus.Canceled:
                                completer.SetCancelled();
                                break;

                            case TaskStatus.Faulted:
                                ShinyHost
                                    .LoggerFactory
                                    .CreateLogger<ILogger<IJobManager>>()
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
}
