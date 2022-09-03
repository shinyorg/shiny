using System;
using CoreLocation;

namespace Shiny.Locations;


public class AppleLocationConfiguration
{
    public CLActivityType? ActivityType { get; set; }
    public bool ShowsBackgroundLocationIndicator { get; set; } = true;
}