using System;
using AndroidX.Core.App;


namespace Shiny.Notifications
{
    public class AndroidPersistentNotification : IPersistentNotification
    {
        readonly NotificationManagerCompat manager;
        readonly NotificationCompat.Builder builder;
        readonly int notificationId;


        public AndroidPersistentNotification(int notificationId, NotificationManagerCompat manager, NotificationCompat.Builder builder)
        {
            this.notificationId = notificationId;
            this.manager = manager;
            this.builder = builder;
        }


        public void SetProgress(int progress, int total)
        {
            this.builder.SetProgress(total, progress, false);
            this.Update();
        }


        public void Dismiss() => this.manager.Cancel(this.notificationId);
        public void Dispose() => this.Dismiss();
        public void SetIndeterministicProgress(bool show)
        {
            this.builder.SetProgress(0, 0, show);
            this.Update();
        }


        public void ClearProgress() => this.SetIndeterministicProgress(false);
        void Update() => this.manager.Notify(this.notificationId, this.builder.Build());
    }
}
