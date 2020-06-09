-----------------
Shiny.Locations
-----------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


-----------------
Shiny Startup
-----------------
// Make sure to follow all of the general Shiny wireup from Shiny.Core

//builder.UseGeofencing<LocationDelegates>(new GeofenceRegion("Test", new Position(1, 1), Distance.FromKilometers(1)));
builder.UseGeofencing<LocationDelegates>();

builder.UseGps<LocationDelegates>();

// motion activity
builder.UseMotionActivity();

-----------------
General Usage
-----------------

Make sure to ask for permission FIRST on whatever service you are using.  GpsManager, GeofenceManager, and MotionActivityManager all have their version of RequestAccess

var access = await Service.RequestAccess();

Geofencing Samples: https://github.com/shinyorg/shinysamples/tree/master/Samples/Geofences
GPS Samples: https://github.com/shinyorg/shinysamples/tree/master/Samples/Gps
Motion Activity Samples: https://github.com/shinyorg/shinysamples/tree/master/Samples/MotionActivity

-----------------
iOS Setup
-----------------

The following is required for GPS & Geofencing
---
<key>NSLocationAlwaysUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>
<key>NSLocationWhenInUseUsageDescription</key>
<string>The beacons or geofences or GPS always have you!</string>


Required for Geofencing & Background GPS
---
<key>UIBackgroundModes</key>
<array>
	<string>location</string>
</array>

The following is required for Motion Activity
---
<key>NSMotionUsageDescription</key>
<string>Required for pedometer</string>

-----------------
Android SEtup
-----------------
Testing on Android Simulators can be difficult and doesn't always work out of the box!  It is advised to test on devices first.

<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />

<!--Android 10+-->
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />

-----------------
UWP Setup
-----------------

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="location" />
    </BackgroundTasks>
</Extension>