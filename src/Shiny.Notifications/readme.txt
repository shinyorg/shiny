-------------------
Shiny.Notifications
-------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


----------
SETUP
----------


1. Create a Startup.cs file in your core/abstraction/.NET standard

public class YourStartup : Shiny.ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // register your shiny services here
        services.UseNotifications(); // set true
    }
}


-----------------
Android
-----------------

Schedulated notifications on Android use Shiny background jobs - so use the following setup:

1. Create an application in your android head project

using System;
using Shiny;
using Android.App;
using Android.Runtime;


[Application]
public class YourApplication : ShinyAndroidApplication<YourStartup>
{
    public YourApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
    {
    }
}


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


3. Be sure to add a notifications.png OR set the Shiny.Notifications.AndroidOptions.SmallIconResource = "YourIconResourceName";


-----------------
UWP
-----------------

Schedulated notifications on UWP use Shiny background jobs - so use the following setup:


1. Add the following to your App.xaml.cs constructor

Shiny.UwpShinyHost.Init(new YourStartup());


2. Add the following to your Package.appxmanifest under the <Application><Extensions> node

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>

------
Tizen
------
<privileges>
   <privilege>http://tizen.org/privilege/notification</privilege>
</privileges>