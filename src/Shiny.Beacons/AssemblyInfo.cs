using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.Beacons.IBeaconMonitorDelegate", "UseBeaconMonitoring", true)]
[assembly: AutoStartup("UseBeaconRanging")]

[assembly: StaticGeneration("Shiny.Beacons.IBeaconRangingManager", "ShinyBeaconRanging")]
[assembly: StaticGeneration("Shiny.Beacons.IBeaconMonitoringManager", "ShinyBeaconMonitoring")]

