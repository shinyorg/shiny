using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Locations.Extensions.AI.Internal;

/// <summary>Reads the device's current (last cached) GPS position and motion state.</summary>
sealed class GetCurrentLocationFunction : LocationAIFunctionBase
{
    public GetCurrentLocationFunction(IGpsManager gps)
        : base(gps, "get_current_location",
            "Get the device's current location (latitude/longitude) plus accuracy, altitude, speed, heading, and the reading timestamp. Uses the last cached GPS fix.",
            AiSchema.ToElement(AiSchema.Object(new JsonObject())))
    { }

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var (reading, error) = await this.GetReadingAsync().ConfigureAwait(false);
        if (error != null)
            return error;

        var r = reading!;
        return new JsonObject
        {
            ["latitude"] = r.Position.Latitude,
            ["longitude"] = r.Position.Longitude,
            ["accuracyMeters"] = r.PositionAccuracy,
            ["altitudeMeters"] = r.Altitude,
            ["speedMetersPerSecond"] = r.Speed,
            ["headingDegrees"] = r.Heading,
            ["isStationary"] = r.IsStationary,
            ["timestamp"] = r.Timestamp.ToString("o", CultureInfo.InvariantCulture)
        };
    }
}

/// <summary>Computes great-circle distance and bearing from the current location to a destination.</summary>
sealed class GetDistanceToFunction : LocationAIFunctionBase
{
    public GetDistanceToFunction(IGpsManager gps)
        : base(gps, "get_distance_to",
            "Compute the straight-line (great-circle) distance and compass bearing from the device's current location to a destination latitude/longitude. This is as-the-crow-flies, not a routed driving/walking distance.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["latitude"] = AiSchema.Number("Destination latitude in decimal degrees (-90 to 90)."),
                ["longitude"] = AiSchema.Number("Destination longitude in decimal degrees (-180 to 180).")
            },
            "latitude", "longitude"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        if (!TryGetDestination(arguments, out var dest, out var destError))
            return destError;

        var (reading, error) = await this.GetReadingAsync().ConfigureAwait(false);
        if (error != null)
            return error;

        var here = reading!.Position;
        var distance = here.GetDistanceTo(dest);
        var bearing = here.GetCompassBearingTo(dest);

        return new JsonObject
        {
            ["distanceKilometers"] = Math.Round(distance.TotalKilometers, 3),
            ["distanceMiles"] = Math.Round(distance.TotalMiles, 3),
            ["distanceMeters"] = Math.Round(distance.TotalMeters, 1),
            ["bearingDegrees"] = Math.Round(bearing, 1),
            ["note"] = "Great-circle (straight-line) distance, not a routed travel distance."
        };
    }

    internal static bool TryGetDestination(AIFunctionArguments args, out Position dest, out JsonObject? error)
    {
        dest = default!;
        error = null;

        var lat = GetDouble(args, "latitude");
        var lng = GetDouble(args, "longitude");
        if (lat is null || lng is null)
        {
            error = new JsonObject { ["error"] = "'latitude' and 'longitude' are required." };
            return false;
        }
        if (lat is < -90 or > 90 || lng is < -180 or > 180)
        {
            error = new JsonObject { ["error"] = "latitude must be -90..90 and longitude -180..180." };
            return false;
        }

        dest = new Position(lat.Value, lng.Value);
        return true;
    }
}

/// <summary>Estimates travel time from the current location to a destination for a chosen mode.</summary>
sealed class EstimateTravelTimeFunction : LocationAIFunctionBase
{
    // rough average speeds in km/h for a straight-line estimate
    static readonly IReadOnlyDictionary<string, double> ModeSpeeds = new Dictionary<string, double>
    {
        ["walking"] = 5,
        ["cycling"] = 15,
        ["transit"] = 30,
        ["driving"] = 50
    };

    public EstimateTravelTimeFunction(IGpsManager gps)
        : base(gps, "estimate_travel_time",
            "Estimate how long it takes to reach a destination from the device's current location. The estimate uses straight-line distance and an average speed for the chosen mode (or a speedKmh override), so it is a rough approximation - not a routed ETA with real roads or traffic.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["latitude"] = AiSchema.Number("Destination latitude in decimal degrees (-90 to 90)."),
                ["longitude"] = AiSchema.Number("Destination longitude in decimal degrees (-180 to 180)."),
                ["mode"] = AiSchema.String("Travel mode used to pick an average speed.", ModeSpeeds.Keys),
                ["speedKmh"] = AiSchema.Number("Optional override for the average speed in km/h. Takes precedence over mode.")
            },
            "latitude", "longitude"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        if (!GetDistanceToFunction.TryGetDestination(arguments, out var dest, out var destError))
            return destError;

        var mode = GetString(arguments, "mode")?.ToLowerInvariant() ?? "driving";
        var speedKmh = GetDouble(arguments, "speedKmh");
        if (speedKmh is null && !ModeSpeeds.TryGetValue(mode, out var defaultSpeed))
            return new JsonObject { ["error"] = $"Unknown mode '{mode}'. Use one of: {string.Join(", ", ModeSpeeds.Keys)}, or pass speedKmh." };

        var speed = speedKmh ?? ModeSpeeds[mode];
        if (speed <= 0)
            return new JsonObject { ["error"] = "speedKmh must be greater than zero." };

        var (reading, error) = await this.GetReadingAsync().ConfigureAwait(false);
        if (error != null)
            return error;

        var distance = reading!.Position.GetDistanceTo(dest);
        var hours = distance.TotalKilometers / speed;
        var minutes = hours * 60;

        return new JsonObject
        {
            ["distanceKilometers"] = Math.Round(distance.TotalKilometers, 3),
            ["distanceMiles"] = Math.Round(distance.TotalMiles, 3),
            ["mode"] = speedKmh != null ? "custom" : mode,
            ["assumedSpeedKmh"] = speed,
            ["estimatedMinutes"] = Math.Round(minutes, 1),
            ["note"] = "Straight-line estimate using an average speed - not a routed ETA (no roads or traffic)."
        };
    }
}
