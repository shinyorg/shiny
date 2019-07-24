Title: iOS
---
# Beacons - iOS

iOS has its own official iBeacon API that of course uses BLE under the hood, but all of its permission set is based on Locations.  

## Setup

In your Info.plist, add the following:

```xml
	<key>NSLocationAlwaysUsageDescription</key>
	<string>The beacons or geofences or GPS always have you!</string>
	<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
	<string>The beacons or geofences or GPS always have you!</string>
	<key>NSLocationWhenInUseUsageDescription</key>
	<string>The beacons or geofences or GPS always have you!</string>
```

If you also want to monitoring (background searching for beacons), you'll also need to add the following node to your Info.plist UIBackground modes

```xml
	<key>UIBackgroundModes</key>
	<array>
		<string>location</string>
	</array>
```


## Requesting Permissions


## Gotchas

* iOS limits the amount of beacons you are allowed to monitor to 20.  This value is also shared with your geofences, so you need to think about your strategy when using beacons

