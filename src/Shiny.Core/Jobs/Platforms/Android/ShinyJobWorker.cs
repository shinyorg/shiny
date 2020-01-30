#if ANDROIDX
using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using AndroidX.Concurrent.Futures;
using AndroidX.Work;
using Google.Common.Util.Concurrent;
using Shiny.Logging;


namespace Shiny.Jobs
{
    public class ShinyJobWorker : ListenableWorker, CallbackToFutureAdapter.IResolver
    {
        public const string ShinyJobIdentifier = nameof(ShinyJobIdentifier);


        public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }


        public Java.Lang.Object AttachCompleter(CallbackToFutureAdapter.Completer completer)
        {
            var jobName = this.InputData.GetString(ShinyJobIdentifier);
            if (jobName.IsEmpty())
            {
                completer.Set(null);
            }
            else
            { 
                ShinyHost
                    .Resolve<IJobManager>()
                    .Run(jobName, CancellationToken.None)
                    .ContinueWith(x =>
                    {
                        switch (x.Status)
                        {
                            case TaskStatus.Canceled:
                                completer.SetCancelled();
                                break;

                            case TaskStatus.Faulted:
                                Log.Write(x.Exception);
                                completer.SetException(new Java.Lang.Throwable(x.Exception.ToString()));
                                break;

                            case TaskStatus.RanToCompletion:
                                completer.Set(null);
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
            base.OnStopped();
        }
    }
}
#endif