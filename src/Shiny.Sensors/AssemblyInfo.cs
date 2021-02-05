using Shiny.Attributes;

[assembly: AutoStartup("services.UseAllSensors")]

[assembly: StaticGeneration("Shiny.Sensors.IAccelerometer", "ShinyAccelerometer")]
[assembly: StaticGeneration("Shiny.Sensors.IAmbientLight", "ShinyAmbientLight")]
[assembly: StaticGeneration("Shiny.Sensors.IBarometer", "ShinyBarometer")]
[assembly: StaticGeneration("Shiny.Sensors.ICompass", "ShinyCompass")]
[assembly: StaticGeneration("Shiny.Sensors.IGyroscope", "ShinyGyroscope")]
[assembly: StaticGeneration("Shiny.Sensors.IHeartRateMonitor", "ShinyHeartRate")]
[assembly: StaticGeneration("Shiny.Sensors.IHumidity", "ShinyHumidity")]
[assembly: StaticGeneration("Shiny.Sensors.IMagnetometer", "ShinyMagnetometer")]
[assembly: StaticGeneration("Shiny.Sensors.IPedometer", "ShinyPedometer")]
[assembly: StaticGeneration("Shiny.Sensors.IProximity", "ShinyProximity")]
[assembly: StaticGeneration("Shiny.Sensors.ITemperature", "ShinyTemperature")]
