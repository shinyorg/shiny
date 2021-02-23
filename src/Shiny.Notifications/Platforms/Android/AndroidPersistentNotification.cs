using System;
using AndroidX.Core.App;


namespace Shiny.Notifications
{
    public class AndroidPersistentNotification : IPersistentNotification
    {
        readonly AndroidNotificationManager manager;
        readonly NotificationCompat.Builder builder;
        static int currentId = 8999;
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

            this.notificationId = ++currentId;
            this.manager.NativeManager.Notify(this.notificationId, this.builder.Build());
            this.IsShowing = true;
        }


        public void Dismiss()
        {
            this.manager.NativeManager.Cancel(this.notificationId);
            this.IsShowing = false;
        }


        public void Dispose() => this.Dismiss();


        string title;
        public string Title
        {
            get => this.title;
            set
            {
                if (this.title == value)
                    return;

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
                if (this.message == value)
                    return;

                this.message = value;
                this.builder.SetContentText(value);
                this.Update();
            }
        }


        bool ind;
        public bool IsIndeterministic
        {
            get => this.ind;
            set
            {
                if (this.ind == value)
                    return;

                this.ind = value;
                this.SetProgress();
            }
        }


        double total;
        public double Total
        {
            get => this.total;
            set
            {
                if (this.total == value)
                    return;

                this.total = value;
                this.SetProgress();
            }
        }


        double progress;
        public double Progress
        {
            get => this.progress;
            set
            {
                if (this.progress == value)
                    return;

                this.progress = value;
                this.SetProgress();
            }
        }


        public bool IsShowing { get; private set; }


        void SetProgress()
        {
            var progress = Convert.ToInt32(this.Progress * 100);
            var total = Convert.ToInt32(this.Total * 100);

            this.builder.SetProgress(total, progress, this.IsIndeterministic);
            this.Update();
        }


        void Update()
        {
            if (this.IsShowing)
                this.manager.NativeManager.Notify(this.notificationId, this.builder.Build());
        }
    }
}
