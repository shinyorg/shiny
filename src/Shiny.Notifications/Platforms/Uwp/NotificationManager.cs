using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Shiny.Jobs;
using Shiny.Settings;
using Shiny.Infrastructure;


namespace Shiny.Notifications
{
    //https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/08/quickstart-sending-a-local-toast-notification-and-handling-activations-from-it-windows-10/
    public class NotificationManager : INotificationManager
    {
        readonly ToastNotifier toastNotifier;
        readonly IRepository repository;
        readonly IJobManager jobs;
        readonly ISettings settings;


        public NotificationManagerImpl(IJobManager jobs,
                                       ISettings settings,
                                       IRepository repository)
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();
            this.jobs = jobs;
            this.settings = settings;
            this.repository = repository;
        }


        public Task<AccessState> RequestAccess()
            => this.jobs.RequestAccess();


        public async Task Send(Notification notification)
        {
            if (notification.Id == 0)
                notification.Id = this.settings.IncrementValue("NotificationId");

            if (notification.ScheduleDate != null)
            {
                await this.repository.Set(notification.Id.ToString(), notification);
                return;
            }

            var toastContent = new ToastContent
            {
                Duration = notification.Windows.UseLongDuration ? ToastDuration.Long : ToastDuration.Short,
                Launch = notification.Payload,
                Visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText
                            {
                                Text = notification.Title
                            },
                            new AdaptiveText
                            {
                                Text = notification.Message
                            }
                        }
                    }
                }
            };

            if (!notification.Sound.IsEmpty())
            {
                var sound = this.BuildSoundPath(notification.Sound);
                toastContent.Audio = new ToastAudio
                {
                    Src = new Uri(sound)
                };
            }
            //toastContent.Actions
            //toastContent.AdditionalProperties.Ad
            //toastContent.Launch = "";
            var native = new ToastNotification(toastContent.GetXml());
            this.toastNotifier.Show(native);
        }


        string BuildSoundPath(string sound)
        {
            var ext = Path.GetExtension(sound);
            if (String.IsNullOrWhiteSpace(ext))
                sound += ".mp4";

            if (sound.StartsWith("ms-appx://"))
                sound = "ms-appx://" + sound;

            return sound;
        }


        public Task Clear() => this.repository.Clear<Notification>();
        public async Task<IEnumerable<Notification>> GetPending() => await this.repository.GetAll<Notification>();
        public Task Cancel(int id) => this.repository.Remove<Notification>(id.ToString());
    }
}