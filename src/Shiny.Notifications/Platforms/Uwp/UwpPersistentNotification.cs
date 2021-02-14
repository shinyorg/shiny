using System;
using System.Threading;
using Windows.UI.Notifications;


namespace Shiny.Notifications
{
    public class UwpPersistentNotification : IPersistentNotification
    {
        readonly ToastNotifier toastNotifier;
        readonly Notification notification;
        int total = 0;
        int progress = 0;
        int sequence = 1;
        bool indeterministic;


        public UwpPersistentNotification(Notification notification, ToastNotifier toastNotifier)
        {
            this.notification = notification;
            this.toastNotifier = toastNotifier;
        }


        public void ClearProgress()
        {
            this.total = 0;
            this.progress = 0;
            this.Update();
        }


        public void Dismiss() => ToastNotificationManager.History.Remove(this.notification.Id.ToString());
        public void Dispose() => this.Dismiss();


        public void SetIndeterministicProgress(bool show)
        {
            this.indeterministic = true;
            this.Update();
        }


        public void SetProgress(int progress, int total)
        {
            this.indeterministic = false;
            this.progress = progress;
            this.total = total;
            this.Update();
        }


        void Update()
        {
            Interlocked.Increment(ref this.sequence);
            var data = new NotificationData
            {
                SequenceNumber = (uint)this.sequence
            };
            //data.Values["progressStatus"] = this.notification.Title;

            if (!this.indeterministic)
            {
                data.Values["progressValue"] = (this.progress / this.total).ToString();
                data.Values["progressValueString"] = $"{this.progress}/{this.total}";
            }
            this.toastNotifier.Update(
                data,
                this.notification.Id.ToString(),
                notification.Channel
            );
        }
    }
}
