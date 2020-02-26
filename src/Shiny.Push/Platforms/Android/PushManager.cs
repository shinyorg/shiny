using System;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Android.Runtime;
using Firebase.Iid;
using Firebase.Messaging;
using Shiny.Settings;
using Task = System.Threading.Tasks.Task;
using CancellationToken = System.Threading.CancellationToken;
using Android.Gms.Common;
using System.Collections.Generic;

namespace Shiny.Push
{
    public class PushManager : Java.Lang.Object, IOnCompleteListener, IPushManager
    {
        readonly AndroidContext context;
        readonly ISettings settings;
        readonly IMessageBus bus;        
        TaskCompletionSource<string>? taskSrc = null;


        public PushManager(AndroidContext context, ISettings settings, IMessageBus bus)
        {
            this.context = context;
            this.settings = settings;
            this.bus = bus;
        }


        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (!task.IsSuccessful)
                this.taskSrc?.TrySetException(task.Exception);
            else
            {
                var result = task.Result.JavaCast<IInstanceIdResult>();
                this.taskSrc?.TrySetResult(result.Token);
            }   
        }


        public string? CurrentRegistrationToken
        {
            get => this.settings.Get<string?>(nameof(CurrentRegistrationToken));
            protected set => this.settings.SetRegToken(value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.settings.Get<DateTime?>(nameof(CurrentRegistrationTokenDate));
            protected set => this.settings.SetRegDate(value);
        }


        public virtual async Task<PushAccessState> RequestAccess(CancellationToken cancelToken = default)
        {
            //var resultCode = GoogleApiAvailability
            //    .Instance
            //    .IsGooglePlayServicesAvailable(this.context.AppContext);

            //if (resultCode != ConnectionResult.ServiceMissing)
            //{ 
            ////{
            ////    if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
            ////        msgText.Text = GoogleApiAvailability.Instance.GetErrorString(resultCode);
            //}
            this.taskSrc = new TaskCompletionSource<string>();
            using (cancelToken.Register(() => this.taskSrc.TrySetCanceled()))
            {
                FirebaseInstanceId
                    .Instance
                    .GetInstanceId()
                    .AddOnCompleteListener(this);

                this.CurrentRegistrationToken = await this.taskSrc.Task;
                this.CurrentRegistrationTokenDate = DateTime.UtcNow;
                FirebaseMessaging.Instance.AutoInitEnabled = true;

                return new PushAccessState(AccessState.Available, this.CurrentRegistrationToken);
            }
        }


        public virtual async Task UnRegister()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;

            // must be executed off proc
            await Task.Run(() => FirebaseInstanceId.Instance.DeleteInstanceId());
        }

        public IObservable<IDictionary<string, string>> WhenReceived()
            => this.bus.Listener<Dictionary<string, string>>(nameof(ShinyFirebaseService));
    }
}
