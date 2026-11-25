namespace Shiny.Locations;


/// <summary>
/// GeoClue's accuracy levels (<c>GClueAccuracyLevel</c>). The level decides which sources GeoClue may use:
/// country and city come from the IP address alone, street from Wi-Fi, exact from a GPS receiver or modem.
/// </summary>
public enum GeoClueAccuracyLevel : uint
{
    /// <summary>No location.</summary>
    None = 0,
    /// <summary>Country level, from the IP address.</summary>
    Country = 1,
    /// <summary>City level, from the IP address.</summary>
    City = 4,
    /// <summary>Neighbourhood level.</summary>
    Neighborhood = 5,
    /// <summary>Street level, from Wi-Fi positioning.</summary>
    Street = 6,
    /// <summary>The best available, from a GPS receiver or modem.</summary>
    Exact = 8
}
