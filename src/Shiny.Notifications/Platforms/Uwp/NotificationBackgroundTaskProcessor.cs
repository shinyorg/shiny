using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;


namespace Shiny.Notifications
{
    public class NotificationBackgroundTaskProcessor : IBackgroundTaskProcessor
    {
        readonly ISerializer serializer;
        readonly IEnumerable<INotificationDelegate> delegates;
        readonly ILogger logger;


        public NotificationBackgroundTaskProcessor(ISerializer serializer,
                                                   IEnumerable<INotificationDelegate> delegates,
                                                   ILogger<INotificationDelegate> logger)
        {
            this.serializer = serializer;
            this.delegates = delegates;
            this.logger = logger;
        }


        public async void Process(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details == null)
                return;

            try
            {
                //details.UserInput["Text"];
                //this.delegates.RunDelegates(x => x.OnEntry(null));

                //string arguments = details.Argument;
                //if (arguments == null || !arguments.Equals("quickReply"))
                //{
                //    ToastHelper.PopToast("ERROR", $"Expected arguments to be 'quickReply' but was '{arguments}'.");
                //    return;
                //}

                //var result = details.UserInput;

                //if (result.Count != 1)
                //    ToastHelper.PopToast("ERROR", "ERROR: Expected 1 user input value, but there were " + result.Count);

                //else if (!result.ContainsKey("message"))
                //    ToastHelper.PopToast("ERROR", "ERROR: Expected a user input value for 'message', but there was none.");

                //else if (!(result["message"] as string).Equals("Windows 10"))
                //    ToastHelper.PopToast("ERROR", "ERROR: User input value for 'message' was not 'Windows 10'");

                //else
                //{
                //    ToastHelper.PopToast("SUCCESS", "This scenario successfully completed. Please mark it as passed.");
                //}


                //var nativeNotifications = await UserNotificationListener.Current.GetNotificationsAsync(NotificationKinds.Toast);
                //foreach (var native in nativeNotifications)
                //{
                //    //native.Notification.Visual.Bindings.
                //}
                    //ndelegate.OnEntry()
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "");
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
/*
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
