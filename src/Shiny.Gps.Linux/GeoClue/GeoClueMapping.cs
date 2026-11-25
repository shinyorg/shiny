namespace Shiny.Locations.GeoClue;


/// <summary>
/// The pure decisions behind the GeoClue manager, kept apart from D-Bus so they can be tested.
/// </summary>
internal static class GeoClueMapping
{
    /// <summary>The level to ask GeoClue for. Background modes don't change it - see <see cref="LinuxGpsManager"/>.</summary>
    public static GeoClueAccuracyLevel AccuracyFor(GpsRequest request, LinuxGpsOptions options)
        => request.RequestPreciseAccuracy ? options.PreciseAccuracyLevel : options.AccuracyLevel;


    /// <summary>
    /// What a D-Bus error from GeoClue says about access, or null when it says nothing (a timeout, a
    /// transient failure) and the last known state should stand.
    /// </summary>
    public static AccessState? AccessFromError(string? errorName) => errorName switch
    {
        // location services switched off, or the agent (GNOME's privacy setting, a portal) said no
        "org.freedesktop.DBus.Error.AccessDenied" => AccessState.Denied,
        // no GeoClue on this system at all
        "org.freedesktop.DBus.Error.ServiceUnknown" => AccessState.NotSupported,
        "org.freedesktop.DBus.Error.NameHasNoOwner" => AccessState.NotSupported,
        _ => null
    };


    /// <summary>
    /// A reading from GeoClue's Location properties. GeoClue marks unknowns with sentinels: altitude
    /// -G_MAXDOUBLE, speed and heading -1. Altitude becomes 0; speed and heading stay -1, the same
    /// "invalid" CoreLocation reports.
    /// </summary>
    public static GpsReading ToReading(
        double latitude,
        double longitude,
        double accuracy,
        double altitude,
        double speed,
        double heading,
        ulong timestampSeconds,
        ulong timestampMicroseconds,
        DateTimeOffset now
    )
    {
        var timestamp = timestampSeconds == 0
            ? now
            : DateTimeOffset.FromUnixTimeSeconds((long)timestampSeconds).AddTicks((long)timestampMicroseconds * 10);

        return new GpsReading(
            new Position(latitude, longitude),
            accuracy,
            timestamp,
            heading < 0 ? -1 : heading,
            0,
            altitude <= -double.MaxValue / 2 ? 0 : altitude,
            speed < 0 ? -1 : speed,
            0
        );
    }
}
