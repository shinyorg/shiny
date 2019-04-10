#if ANDROID9
using System;
using Android.Content;
using AndroidX.Work;


namespace Shiny.Jobs
{
    public class ShinyJobWorker : Worker
    {
        public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }

        //public ShinyJobWorker(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        //{
        //}

        public override Result DoWork()
        {
            return Result.InvokeSuccess();
        }
    }
}
#endif