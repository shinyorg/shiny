Title: Monitoring
---

Beacon monitoring is quite a bit different than ranging.  Monitoring does not provide distance and has many limitations, but it works in the background.  Monitoring uses beacon regions much like ranging, however, the region is all that is returned in the delegate to protect user privacy


Monitoring is limited to a maximum of 20 regions on iOS.  On Android & UWP, there currently isn't a limit HOWEVER the BLE scans do emit quite a number of scan packets.  If you have too many, your scan will slow down your app quite a bit.

|Area|Info|
|----|----|
|NuGet| [![BeaconsNugetShield]][BeaconsNuget] |
|Shiny Startup|services.UseBeaconMonitoring<YourBeaconDelegate>()|
|Main Service|Shiny.Beacons.IBeaconMonitoringManager|
|Background Delegate (required)|Shiny.Beacons.IBeaconMonitoringDelegate (required)|
|Static Generated|ShinyBeaconMonitoring|
|Manual Resolve|ShinyHost.Resolve<Shiny.Beacons.IBeaconMonitoringManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Beacons.IBeaconMonitoringManager>()|
