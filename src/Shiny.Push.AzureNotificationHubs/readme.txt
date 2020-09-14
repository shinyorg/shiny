-------------------------------------
Shiny.Push.AzureNotificationHubs
-------------------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------------
Setup
-----------------------

Follow the document here to setup push on Azure Notification Hubs as well as setting up push on your platforms - https://docs.microsoft.com/en-us/azure/developer/mobile-apps/notification-hubs-backend-service-xamarin-forms

public class SampleStartup : Startup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UsePushAzureNotificationHubs<PushDelegate>(
            "Your Listener Connection String",
            "Your Hub Name"
        );
    }
}


-------
Android
-------
Android requires a few more additional steps

Ensure the following libraries are installed in your ANDROID HEAD PROJECT

<PackageReference Include="Xamarin.Firebase.Messaging" Version="120.1.7-preview02" />
<PackageReference Include="Xamarin.GooglePlayServices.Tasks" Version="117.0.2-preview02" />


2. In your Main/Launch Activity (or every activity where you are going to ask for permissions).  Shiny will attempt to wire these automatically if you mark the activity as partial

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
