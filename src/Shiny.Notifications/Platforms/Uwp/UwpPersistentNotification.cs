using System;
using System.Threading;
using Windows.UI.Notifications;


namespace Shiny.Notifications
{
    public class UwpPersistentNotification : IPersistentNotification
    {
        readonly ToastNotifier toastNotifications;
        readonly Notification notification;
        int sequence = 1;


        public UwpPersistentNotification(Notification notification)
        {
            this.notification = notification;
            this.toastNotifications = ToastNotificationManager.CreateToastNotifier();
        }


        public void ClearProgress()
        {
        }


        public void Dismiss() => ToastNotificationManager.History.Remove(this.notification.Id.ToString());
        public void Dispose() => this.Dismiss();


        public void SetIndeterministicProgress(bool show)
        {
        }


        public void SetProgress(int progress, int total)
        {
        }


        void Update()
        {
            Interlocked.Increment(ref this.sequence);
            var data = new NotificationData
            {
                SequenceNumber = (uint)this.sequence
            };

            // Assign new values
            // Note that you only need to assign values that changed. In this example
            // we don't assign progressStatus since we don't need to change it
            //data.Values["progressValue"] = progress / total;
            //data.Values["progressValueString"] = "18/26 songs";
            //data.Values["progressStatus"] = "Downloading...";

            // Update the existing notification's data by using tag/group
            this.toastNotifications.Update(
                data,
                this.notification.Id.ToString(),
                notification.Channel
            );
        }
    }
}
