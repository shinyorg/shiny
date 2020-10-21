using System;
using System.Threading.Tasks;
using Android.Content;
using Shiny.Logging;


namespace Shiny
{
    public abstract class ShinyBroadcastReceiver : BroadcastReceiver
    {
        protected virtual void Execute(Func<Task> task)
        {
            var pendingResult = this.GoAsync();
            task().ContinueWith(x =>
            {
                if (x.IsFaulted)
                    Log.Write(x.Exception);

                pendingResult.Finish();
            });
        }
    }
}
