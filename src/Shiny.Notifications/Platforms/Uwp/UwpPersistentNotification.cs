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


        public UwpPersistentNotification(Notification initialNotification)
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();

            this.title = initialNotification.Title;
            this.message = initialNotification.Message;
        }


        public bool IsShowing { get; private set; }


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


        // when this changes, if showing, the whole toast changes
        bool? ind;
        public bool? IsIndeterministic
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


        int total;
        public int Total
        {
            get => this.total;
            set
            {
                this.total = value;
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
            data.Values["title"] = this.Title;

            //if (this.indeterministic)
            //{
            //    data.Values["progressValue"] = AdaptiveProgressBarValue.Indeterminate.ToString();
            //}
            //else
            //{
            //    data.Values["progressValue"] = (this.progress / this.total).ToString();
            //    data.Values["progressValueString"] = $"{this.progress}/{this.total}";
            //}
            this.toastNotifier.Update(
                data,
                this.notificationId.ToString(),
                "this.channel"
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

            if (this.IsIndeterministic != null)
            {
                //toastContent.Visual.BindingGeneric.Children.Add(new AdaptiveProgressBar
                //{
                //    Title = new BindableString("title"),
                //    Value = new BindableProgressBarValue("progressValue"),
                //    ValueStringOverride = new BindableString("progressValueString"),
                //    Status = new BindableString("progressStatus")
                //});
            }

            this.toastNotifier.Show(new ToastNotification(toastContent.GetXml())
            {
                Tag = this.notificationId.ToString()
                //Group = notification.Channel ?? Channel.Default.Identifier
            });
        }
    }
}

//protected virtual ToastNotification CreateNativeNotification(ToastContent toastContent, Notification notification)
//    =>