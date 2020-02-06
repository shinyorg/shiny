using System;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Android.Runtime;
using Firebase;
using Firebase.Iid;
using Firebase.Messaging;
using Shiny.Settings;
using Task = System.Threading.Tasks.Task;


namespace Shiny.Push
{
    public class PushManager : Java.Lang.Object, IOnCompleteListener, IPushManager
    {
        readonly ISettings settings;
        TaskCompletionSource<string>? taskSrc = null;


        public PushManager(ISettings settings, AndroidContext context)
        {
            this.settings = settings;
            FirebaseApp.InitializeApp(context.AppContext);
        }


        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (task.IsSuccessful)
                this.taskSrc?.TrySetResult(task.Result.JavaCast<IInstanceIdResult>().Token);
            else
                this.taskSrc?.TrySetException(task.Exception);
        }


        public string? CurrentRegistrationToken
        {
            get => this.settings.Get<string?>(nameof(CurrentRegistrationToken));
            protected set => this.settings.Set(nameof(CurrentRegistrationToken), value);
        }


        public DateTime? CurrentRegistrationTokenDate
        {
            get => this.settings.Get<DateTime?>(nameof(CurrentRegistrationTokenDate));
            protected set => this.settings.Set(nameof(CurrentRegistrationTokenDate), value);
        }


        public virtual async Task<PushAccessState> RequestAccess()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;

            this.taskSrc = new TaskCompletionSource<string>();
            FirebaseInstanceId.Instance.GetInstanceId().AddOnCompleteListener(this);

            var token = await this.taskSrc.Task;

            this.CurrentRegistrationToken = token;
            this.CurrentRegistrationTokenDate = DateTime.UtcNow;

            return new PushAccessState(AccessState.Available, token);
        }


        public virtual Task UnRegister()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            

            return Task.CompletedTask;
        }
    }
}
