-----------------
Shiny.Locations
-----------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


//builder.UseGeofencing<LocationDelegates>(new GeofenceRegion("Test", new Position(1, 1), Distance.FromKilometers(1)));
builder.UseGeofencing<LocationDelegates>();
builder.UseGps<LocationDelegates>();
builder.UseMotionActivity();

-----------------
iOS
-----------------

The following is required for Motion Activity
<key>NSMotionUsageDescription</key>
<string>Required for pedometer</string>

The following is required for GPS & Geofencing
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
Testing on Android Simulators can be difficult and doesn't always work out of the box!  It is advised to test on devices first.

<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />

<!--Android 10+-->
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />

-----------------
UWP
-----------------

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="location" />
    </BackgroundTasks>
</Extension>