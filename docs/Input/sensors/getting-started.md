Title: Getting Started
Order: 1
---


## SETUP

1. Be sure to install the Shiny.Sensors nuget package in your shared code project [![NuGet](https://img.shields.io/nuget/v/Shiny.Sensors.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Sensors/)

2. To use each type of sensor, use the following in your ShinyStartup:

```csharp
using Shiny;
using Microsoft.Extensions.DependencyInjection;

namespace YourNamespace
{
	public class YourStartup : ShinyStarup
	{
		services.UseAccelerometer();
		services.UseAmbientLightSensor();
		services.UseBarometer();
		services.UseCompass();
		services.UseMagnetometer();
		services.UsePedometer();
		services.UseProximitySensor();
		services.UseHeartRateMonitor();
		services.UseTemperature();
		services.UseHumidity();	
	}
}
```

> NOTE: each Use<Sensor> returns a boolean as to whether or not it is supported.  You can use optional dependencies (ie. IDependency dep = null) in your viewmodels/services then check for null to see if the sensor is available on the device.