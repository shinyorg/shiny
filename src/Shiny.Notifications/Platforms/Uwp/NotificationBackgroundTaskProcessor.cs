using System;
using Shiny.Infrastructure;
using Shiny.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace Shiny.Notifications
{
    public class NotificationBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly ISerializer serializer;
        readonly INotificationDelegate ndelegate;


        public NotificationBackgroundTaskProcessor(ISerializer serializer, INotificationDelegate ndelegate)
        {
            this.serializer = serializer;
            this.ndelegate = ndelegate;
        }


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            try
            {
                var nativeNotifications = await UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast);
                foreach (var native in nativeNotifications)
                {
                    //native.Notification.Visual.Bindings.
                }
                //this.ndelegate.OnEntry()
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
/*
 * // Get the toast binding, if present
NotificationBinding toastBinding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);

if (toastBinding != null)
{
    // And then get the text elements from the toast binding
    IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();

    // Treat the first text element as the title text
    string titleText = textElements.FirstOrDefault()?.Text;

    // We'll treat all subsequent text elements as body text,
    // joining them together via newlines.
    string bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));
}


// Get the listener
UserNotificationListener listener = UserNotificationListener.Current;

// And request access to the user's notifications (must be called from UI thread)
UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

switch (accessStatus)
{
    // This means the user has granted access.
    case UserNotificationListenerAccessStatus.Allowed:

        // Yay! Proceed as normal
        break;

    // This means the user has denied access.
    // Any further calls to RequestAccessAsync will instantly
    // return Denied. The user must go to the Windows settings
    // and manually allow access.
    case UserNotificationListenerAccessStatus.Denied:

        // Show UI explaining that listener features will not
        // work until user allows access.
        break;

    // This means the user closed the prompt without
    // selecting either allow or deny. Further calls to
    // RequestAccessAsync will show the dialog again.
    case UserNotificationListenerAccessStatus.Unspecified:

        // Show UI that allows the user to bring up the prompt again
        break;
}*/
