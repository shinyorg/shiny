-----------------
Shiny.Beacons
-----------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------
Shiny Startup
-----------------

public class YourShinyStartup : Shiny.ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // register your shiny services here
        // to do ranging (foreground only)
        services.UseBeacons();

        // OR (not AND) if you want to use beacon monitoring (background) and ranging
        services.UseBeacons<BeaconDelegate>();
    }
}

-----------------
iOS
-----------------

<key>NSLocationAlwaysUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>

<key>UIBackgroundModes</key>
<array>
	<string>location</string>
</array>

-----------------
Android
-----------------
Permissions are included via assembly attributes


-----------------
UWP
-----------------

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="bluetooth" />
    </BackgroundTasks>
</Extension>