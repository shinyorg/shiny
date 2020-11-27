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


If you don't require a ton of configuration, you can also use the generator option by adding

[assembly: Shiny.GenerateStartupAttribute]

----------
ANDROID
----------

1. You must set your "Target Android verion" to "Android 10.0 (API Level 29)" or higher

2. In v1, you required to add an Android application class.  In v2, one is generated for you automatically and will even wireup Xamarin Essentials.

3. In your Main/Launch Activity (or every activity where you are going to ask for permissions), mark the activities as partial and Shiny will auto-add the necessary method with the exception of ShinyOnCreate in your OnCreate method which you must add manually

----------
IOS
----------

* Add the following as the first line in your AppDelegate.cs, v2 will autogenerate the rest of the methods needed in AppDelegate as long as your class is marked as a partial class.

using Shiny;
this.ShinyFinishedLaunching(new YourStartup());


** IOS JOBS **
If you plan to use jobs in iOS, please do the following:

2. Add the following to your Info.plist

<key>UIBackgroundModes</key>
<array>
	<string>fetch</string>
</array>


2b. Shiny also supports iOS 13 background processing.  Add the following to your Info.plist and Shiny will use it instead
<key>UIBackgroundModes</key>
<array>
	<string>fetch</string>
    <string>processing</string>
</array>

<key>BGTaskSchedulerPermittedIdentifiers</key>
<array>
	<string>com.shiny.job</string>
	<string>com.shiny.jobpower</string>
	<string>com.shiny.jobnet</string>
	<string>com.shiny.jobpowernet</string>
</array>

To Test:
* Use Visual Studio for Mac
* Set on debug, deploy to iOS simulator
* Select your iOS head project
* Go to toolbar and find "Run"
* Under run, click "Simulate iOS Background Fetch"


----------
UWP
----------

1. Add the following to your App.xaml.cs constructor

using Shiny;
this.ShinyInit(new YourStartup());


2. Add the following to your Package.appxmanifest under the <Application><Extensions> node

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="general"/>
        <Task Type="systemEvent"/>
        <Task Type="timer"/>
    </BackgroundTasks>
</Extension>
