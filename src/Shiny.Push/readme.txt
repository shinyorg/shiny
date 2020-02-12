-------------------
Shiny.Push
-------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


-------
Setup
-------
When using Push, notifications is automatically registered without a notificationdelegate to prevent duplication of logic firing.

Push will automatically trigger notifications if they are "not" silent, otherwise they will trigger jobs.

-------
iOS
-------
Follow https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/deprecated/remote-notifications-in-ios

Route the following methods like so, in your AppDelegate

    public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        => iOSShinyHost.RegisteredForRemoteNotifications(deviceToken);

    public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
        => iOSShinyHost.FailedToRegisterForRemoteNotifications(error);

-------
Android
-------
Ensure the following libraries are installed in your ANDROID HEAD PROJECT

<PackageReference Include="Xamarin.Android.Support.v4" Version="28.0.0.3" />
<PackageReference Include="Xamarin.Firebase.Common" Version="71.1610.0" />
<PackageReference Include="Xamarin.Firebase.Messaging" Version="71.1740.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Base" Version="71.1610.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Basement" Version="71.1620.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="71.1601.0" />

And follow the setup located here: https://docs.microsoft.com/en-us/xamarin/android/data-cloud/google-messaging/firebase-cloud-messaging

------
UWP
------

TODO