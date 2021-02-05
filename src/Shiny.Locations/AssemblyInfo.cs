using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.Locations.IGpsDelegate", "UseGps", false)]
[assembly: AutoStartupWithDelegate("Shiny.Locations.IGeofenceDelegate", "UseGeofencing", true)]
[assembly: AutoStartup("UseMotionActivity")]


[assembly: StaticGeneration("Shiny.Locations.IMotionActivityManager", "ShinyMotionActivity")]
[assembly: StaticGeneration("Shiny.Locations.IGpsManager", "ShinyGps")]
[assembly: StaticGeneration("Shiny.Locations.IGeofenceManager", "ShinyGeofences")]

