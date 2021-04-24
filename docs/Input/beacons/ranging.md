# RANGING

Ranging is a foreground only operation.  You can scan for "all" beacons with a particular filter set.  Meaning if you only filter by UUID, all beacons under that UUID will be returned.  With ranging, all of the identifier values are provided.  You are 


|Area|Info|
|----|----|
|NuGet| [![BeaconsNugetShield]][BeaconsNuget] |
|Shiny Startup|services.UseBeaconRanging()|
|Main Service|Shiny.Beacons.IBeaconMonitoringManager|
|Background Delegate|none|
|Static Generated|ShinyBeaconRanging|
|Manual Resolve|ShinyHost.Resolve<Shiny.Beacons.IBeaconRangingManager>()|
|Xamarin.Forms|DependencyService.Get<Shiny.Beacons.IBeaconRangingManager>()|

<?! Include "../../nuget.md" /?>