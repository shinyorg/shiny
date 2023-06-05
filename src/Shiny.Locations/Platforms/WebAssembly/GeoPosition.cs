using System;
using System.Text.Json.Serialization;

namespace Shiny.Locations.Blazor;


public class GeoPosition
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("altitudeAccuracy")]
    public long? AltitudeAccuracy { get; set; }

    [JsonPropertyName("timestamp")]
    public long Epoch { get; set; }

    [JsonPropertyName("accuracy")]
    public double? RawAccuracy { get; set; }

    [JsonPropertyName("heading")]
    public double? RawHeading { get; set; }

    [JsonPropertyName("speed")]
    public double? RawSpeed { get; set; }

    [JsonPropertyName("altitude")]
    public double? RawAltitude { get; set; }

    Position? position;
    [JsonIgnore]
    public Position Position => this.position ??= new Position(this.Latitude, this.Longitude);

    [JsonIgnore]
    public DateTimeOffset Timestamp => DateTimeOffset.FromUnixTimeMilliseconds(this.Epoch);
}
