----------------------------------------
Shiny.Integrations.FirebaseNotifications
----------------------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

---
iOS
---

Follow https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/deprecated/remote-notifications-in-ios

Route the following methods like so, in your AppDelegate

public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
    => this.ShinyDidReceiveRemoteNotification(userInfo, completionHandler);

public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
    => this.ShinyRegisteredForRemoteNotifications(deviceToken);

public override void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error)
    => this.ShinyFailedToRegisterForRemoteNotifications(error);

----
Android
---

Ensure the following libraries are installed in your ANDROID HEAD PROJECT

<PackageReference Include="Xamarin.Android.Support.v4" Version="28.0.0.3" />
<PackageReference Include="Xamarin.Firebase.Common" Version="71.1610.0" />
<PackageReference Include="Xamarin.Firebase.Messaging" Version="71.1740.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Base" Version="71.1610.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Basement" Version="71.1620.0" />
<PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="71.1601.0" />


2. In your Main/Launch Activity (or every activity where you are going to ask for permissions)

using Shiny;
public override void OnCreate(Bundle savedInstanceState)
{
    this.ShinyOnCreate();
    base.OnCreate(savedInstanceState);
}

public override void OnNewIntent(Intent intent)
{
    this.ShinyOnNewIntent(intent);
    base.OnNewIntent(intent);
}

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    this.Shiny.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
