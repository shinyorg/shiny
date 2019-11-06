----------
Shiny.Core
----------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


----------
SETUP
----------


1. Create a Startup.cs file in your core/abstraction/.NET standard

using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny;


namespace YourNamespace
{
    public class YourStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // register your shiny services here
        }
    }
}


----------
ANDROID
----------

1. You must set your "Target Android verion" to "Android 9.0 (API Level 28)" or higher

2. Create an application in your android head project

using System;
using Shiny;
using Android.App;
using Android.Runtime;


namespace YourNamespace
{
    [Application]
    public class YourApplication : Shiny.ShinyAndroidApplication<YourStartup>
    {
        public YourApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
        }
    }
}


3. In your Main/Launch Activity (or every activity where you are going to ask for permissions)

public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
{
    Shiny.AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}

----------
IOS
----------

* Add the following as the first line in your AppDelegate.cs - FinishedLaunching method

Shiny.iOSShinyHost.Init(new YourStartup());


** IOS JOBS **
If you plan to use jobs in iOS, please do the following:

1. Add this to your AppDelegate.cs

public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
    => Shiny.Jobs.JobManager.OnBackgroundFetch(completionHandler);

2. Add the following to your Info.plist

<key>UIBackgroundModes</key>
<array>
	<string>fetch</string>
</array>



----------
UWP
----------

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
