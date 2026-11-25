using System.Reflection;

namespace Shiny.Locations;


/// <summary>
/// Settings for the GeoClue2 GPS manager.
/// </summary>
public class LinuxGpsOptions
{
    /// <summary>
    /// The desktop id GeoClue's agent shows and remembers its answer by - the name of the app's
    /// <c>.desktop</c> file without the extension (e.g. <c>com.mycompany.myapp</c>). GeoClue refuses a
    /// client that has none. Defaults to the entry assembly's name.
    /// </summary>
    public string DesktopId { get; set; } = Assembly.GetEntryAssembly()?.GetName().Name ?? "shiny";

    /// <summary>
    /// The level asked for when <see cref="GpsRequest.RequestPreciseAccuracy"/> is false. Street (Wi-Fi
    /// positioning) by default: a "GPS" reading at city level would be an IP guess, kilometres out.
    /// </summary>
    public GeoClueAccuracyLevel AccuracyLevel { get; set; } = GeoClueAccuracyLevel.Street;

    /// <summary>The level asked for when <see cref="GpsRequest.RequestPreciseAccuracy"/> is true.</summary>
    public GeoClueAccuracyLevel PreciseAccuracyLevel { get; set; } = GeoClueAccuracyLevel.Exact;

    /// <summary>GeoClue's DistanceThreshold: metres moved before a new location is sent. 0 sends every change.</summary>
    public uint DistanceThresholdMeters { get; set; }

    /// <summary>GeoClue's TimeThreshold: seconds between new locations at most once. 0 sends every change.</summary>
    public uint TimeThresholdSeconds { get; set; }

    /// <summary>Metres a reading must stay within to count towards <see cref="GpsReading.IsStationary"/>.</summary>
    public int StationaryMetersThreshold { get; set; } = 10;

    /// <summary>Seconds within <see cref="StationaryMetersThreshold"/> before a reading is stationary.</summary>
    public int StationarySecondsThreshold { get; set; } = 30;
}
