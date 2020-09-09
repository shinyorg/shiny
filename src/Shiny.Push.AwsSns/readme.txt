-------------------------------------
Shiny.Push.AwsSns
-------------------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------------
Setup
-----------------------

public class SampleStartup : Startup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UsePushAwsSns<PushDelegate>();
    }
}

---
iOS
---
https://docs.microsoft.com/en-us/azure/notification-hubs/xamarin-notification-hubs-ios-push-notification-apns-get-started

Follow https://docs.microsoft.com/en-us/xamarin/ios/platform/user-notifications/deprecated/remote-notifications-in-ios


----
Android
---
https://docs.microsoft.com/en-us/azure/notification-hubs/xamarin-notification-hubs-push-notifications-android-gcm

Ensure the following libraries are installed in your ANDROID HEAD PROJECT

<PackageReference Include="Xamarin.Firebase.Messaging" Version="120.1.7-preview02" />
<PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="117.0.2-preview02" />


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


---
UWP
---
https://docs.microsoft.com/en-us/azure/notification-hubs/notification-hubs-windows-store-dotnet-get-started-wns-push-notification