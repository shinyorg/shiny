#if ANDROIDX
using System;
using Android.Content;
using AndroidX.Work;


namespace Shiny.Jobs
{
    //public class ShinyJobWorker : AndroidX.Work.ListenableWorker
    public class ShinyJobWorker : AndroidX.Work.ListenableWorker
    {
        public ShinyJobWorker(Context context, WorkerParameters workerParams) : base(context, workerParams)
        {
        }


        //public ShinyJobWorker(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        //{
        //}

        //public override Result DoWork()
        //{
        //    return Result.InvokeSuccess();
        //}

//        public class CallbackWorker extends ListenableWorker
//        {

//    public CallbackWorker(Context context, WorkerParameters params)
//        {
//            super(context, params);
//        }

//        @NonNull
//        @Override
//    public ListenableFuture<Result> startWork()
//        {
//            return CallbackToFutureAdapter.getFuture(completer-> {
//                Callback callback = new Callback() {
//                int successes = 0;

//                @Override
//                   public void onFailure(Call call, IOException e)
//                {
//                    completer.setException(e);
//                }

//                @Override
//                   public void onResponse(Call call, Response response)
//                {
//                    ++successes;
//                    if (successes == 100)
//                    {
//                        completer.set(Result.success());
//                    }
//                }
//            };

//            for (int i = 0; i < 100; ++i)
//            {
//                downloadAsynchronously("https://www.google.com", callback);
//            }
//            return callback;
//        });
//    }
//}
    }
}
#endif