using System;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;


namespace Shiny.Notifications
{
    public class UwpPersistentNotification : IPersistentNotification
    {
        readonly ToastNotifier toastNotifier;
        int notificationId;
        int total = 0;
        int progress = 0;
        int sequence = 1;
        string channel;
        bool indeterministic;


        public UwpPersistentNotification(ToastNotifier toastNotifier, int notificationId, string channel)
        {
            this.toastNotifier = toastNotifier;
            this.notificationId = notificationId;
            this.channel = channel;
            this.Update();
        }


        string title;
        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
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
                this.Update();
            }
        }


        public void ClearProgress()
        {
            this.total = 0;
            this.progress = 0;
            this.Update();
        }


        public void Dismiss() => ToastNotificationManager.History.Remove(this.notificationId.ToString());
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
            data.Values["title"] = this.Title;

            if (this.indeterministic)
            {
                data.Values["progressValue"] = AdaptiveProgressBarValue.Indeterminate.ToString();
            }
            else
            {
                data.Values["progressValue"] = (this.progress / this.total).ToString();
                data.Values["progressValueString"] = $"{this.progress}/{this.total}";
            }
            this.toastNotifier.Update(
                data,
                this.notificationId.ToString(),
                this.channel
            );
        }
    }
}
