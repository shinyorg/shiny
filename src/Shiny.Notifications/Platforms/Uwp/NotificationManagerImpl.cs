using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;


namespace Shiny.Notifications
{
    //https://blogs.msdn.microsoft.com/tiles_and_toasts/2015/07/08/quickstart-sending-a-local-toast-notification-and-handling-activations-from-it-windows-10/
    public class NotificationManagerImpl : INotificationManager
    {
        readonly ToastNotifier toastNotifier;


        public NotificationManagerImpl()
        {
            this.toastNotifier = ToastNotificationManager.CreateToastNotifier();
        }


        public Task<AccessState> RequestAccess() => Task.FromResult(AccessState.Available);


        public Task Send(Notification notification)
        {
            var toastContent = new ToastContent
            {
                Duration = notification.Windows.UseLongDuration ? ToastDuration.Long : ToastDuration.Short,
                //Launch = this.ToQueryString(notification.Metadata),
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

            if (!String.IsNullOrWhiteSpace(notification.Sound) && this.IsAudioSupported)
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

            // TODO
            return Task.CompletedTask;
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

        public Task Clear() => Task.CompletedTask;


        protected virtual bool IsAudioSupported =>
            AnalyticsInfo.VersionInfo.DeviceFamily.Equals("Windows.Desktop") &&
            !ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 2);
    }
}