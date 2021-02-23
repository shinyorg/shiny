using System;
using AndroidX.Core.App;


namespace Shiny.Notifications
{
    public class AndroidPersistentNotification : IPersistentNotification
    {
        readonly AndroidNotificationManager manager;
        readonly NotificationCompat.Builder builder;
        static int currentId = 0;
        int notificationId;


        public AndroidPersistentNotification(AndroidNotificationManager manager, NotificationCompat.Builder builder)
        {
            this.manager = manager;
            this.builder = builder;
        }


        public void Show()
        {
            if (this.IsShowing)
                throw new ArgumentException("Already showing");

            this.IsShowing = true;
            this.notificationId = ++currentId;
            this.Update();
        }


        public void Dismiss()
        {
            this.IsShowing = false;
            this.manager.NativeManager.Cancel(this.notificationId);
        }


        public void Dispose() => this.Dismiss();


        string title;
        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
                this.builder.SetContentTitle(value);
                this.Update();
            }
        }


        string message;
        public string Message
        {
            get => this.message;
            set
            {
                this.message = value;
                this.builder.SetContentInfo(value);
                this.Update();
            }
        }


        bool? ind;
        public bool? IsIndeterministic
        {
            get => this.ind;
            set
            {
                this.ind = value;
                //this.builder.SetProgress(this.Total, this.Progress, this.IsIndeterministic);
                this.Update();
            }
        }


        int total;
        public int Total
        {
            get => this.total;
            set
            {
                this.total = value;
                //this.builder.SetProgress(this.Total, this.Progress, this.IsIndeterministic);
                this.Update();
            }
        }


        int progress;
        public int Progress
        {
            get => this.progress;
            set
            {
                this.progress = value;
                //this.builder.SetProgress(this.Total, this.Progress, this.IsIndeterministic);
                this.Update();
            }
        }


        public bool IsShowing { get; private set; }


        void Update()
        {
            if (this.IsShowing)
                this.manager.NativeManager.Notify(this.notificationId, this.builder.Build());
        }
    }
}
