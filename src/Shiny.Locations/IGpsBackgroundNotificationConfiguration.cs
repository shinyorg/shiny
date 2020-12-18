using System;


namespace Shiny.Locations
{
    public interface IGpsBackgroundNotificationConfiguration
    {
        string? Title { get; set; }
        string? Description { get; set; }
        string? Ticker { get; set; }
    }
}
