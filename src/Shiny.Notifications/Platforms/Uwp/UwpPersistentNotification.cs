using System;
using System.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;


namespace Shiny.Notifications
{
    public class UwpPersistentNotification : IPersistentNotification
    {
        readonly ToastNotifier toastNotifier;
        static int current;
        int notificationId;
        int sequence;
        string channel;


        public UwpPersistentNotification(Notification initialNotification)
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();

            this.title = initialNotification.Title;
            this.message = initialNotification.Message;
            this.channel = initialNotification.Channel ?? Channel.Default.Identifier;
        }


        public bool IsShowing { get; private set; }


        string title;
        public string Title
        {
            get => this.title;
            set
            {
                if (this.title == value)
                    return;

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
                if (this.message == value)
                    return;

                this.message = value;
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
                if (this.IsShowing)
                {
                    this.Dismiss();
                    this.Show();
                }
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
                this.Update();
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
                this.Update();
            }
        }


        public void Show()
        {
            if (this.IsShowing)
                throw new ArgumentException("Toast is already showing");

            this.notificationId = ++current;
            this.CreateToast();
            this.IsShowing = true;
        }
        public void Dismiss() => ToastNotificationManager.History.Remove(this.notificationId.ToString());
        public void Dispose() => this.Dismiss();


        void Update()
        {
            Interlocked.Increment(ref this.sequence);
            var data = new NotificationData
            {
                SequenceNumber = (uint)this.sequence
            };
            data.Values["Title"] = this.Title;
            data.Values["Message"] = this.Message;

            if (!this.IsIndeterministic && this.Total > 0)
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


        void CreateToast()
        {
            var toastContent = new ToastContent
            {
                Duration = ToastDuration.Short,
                ActivationType = ToastActivationType.Foreground,
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = new BindableString("Title")
                            },
                            new AdaptiveText
                            {
                                Text = new BindableString("Message")
                            }
                        }
                    }
                }
            };

            if (this.IsIndeterministic || this.Total > 0)
            {
                //Title = new BindableString("title"),
                var progressBar = new AdaptiveProgressBar();

                if (this.IsIndeterministic)
                {
                    progressBar.Value = AdaptiveProgressBarValue.Indeterminate;
                }
                else
                {
                    progressBar.Value = new BindableProgressBarValue("progressValue");
                    progressBar.ValueStringOverride = new BindableString("progressValueString");
                    progressBar.Status = new BindableString("progressStatus");
                }
                toastContent.Visual.BindingGeneric.Children.Add(progressBar);
            }

            this.toastNotifier.Show(new ToastNotification(toastContent.GetXml())
            {
                Tag = this.notificationId.ToString(),
                Group = this.channel
            });
            this.Update();
        }
    }
}