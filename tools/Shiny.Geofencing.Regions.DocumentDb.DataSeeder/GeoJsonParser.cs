using System.Text.Json;
using Shiny.DocumentDb;

namespace Shiny.Geofencing.Regions.DocumentDb.DataSeeder;

/// <summary>
/// Minimal GeoJSON FeatureCollection reader → Shiny.DocumentDb <see cref="Geometry"/>. Enough to consume
/// the ArcGIS/StatsCan feature services this tool downloads from (Polygon / MultiPolygon / Point).
/// </summary>
static class GeoJsonParser
{
    public static List<ParsedFeature> ParseFeatureCollection(string geoJson)
    {
        using var doc = JsonDocument.Parse(geoJson);
        var features = new List<ParsedFeature>();

        if (!doc.RootElement.TryGetProperty("features", out var featureArray))
            return features;

        foreach (var feature in featureArray.EnumerateArray())
        {
            var geometry = feature.TryGetProperty("geometry", out var geomElement)
                ? ParseGeometry(geomElement)
                : null;

            if (geometry != null)
            {
                var properties = new Dictionary<string, JsonElement>();
                if (feature.TryGetProperty("properties", out var propsElement))
                {
                    foreach (var prop in propsElement.EnumerateObject())
                        properties[prop.Name] = prop.Value.Clone();
                }

                features.Add(new ParsedFeature(geometry, properties));
            }
        }

        return features;
    }

    static Geometry? ParseGeometry(JsonElement elem)
    {
        if (elem.ValueKind == JsonValueKind.Null)
            return null;

        var type = elem.GetProperty("type").GetString();
        var coordinates = elem.GetProperty("coordinates");

        return type switch
        {
            "Polygon" => ParsePolygon(coordinates),
            "MultiPolygon" => ParseMultiPolygon(coordinates),
            "Point" => ParsePoint(coordinates),
            _ => null
        };
    }

    static GeoPolygon ParsePolygon(JsonElement coordinates)
    {
        var rings = new List<GeoPoint[]>();
        foreach (var ring in coordinates.EnumerateArray())
        {
            var coords = new List<GeoPoint>();
            foreach (var point in ring.EnumerateArray())
                coords.Add(new GeoPoint(point[1].GetDouble(), point[0].GetDouble()));
            rings.Add(coords.ToArray());
        }

        return rings.Count <= 1
            ? new GeoPolygon(rings[0])
            : new GeoPolygon(rings[0], rings.Skip(1).ToArray());
    }

    static GeoMultiPolygon ParseMultiPolygon(JsonElement coordinates)
    {
        var polygons = new List<GeoPolygon>();
        foreach (var polyCoords in coordinates.EnumerateArray())
            polygons.Add(ParsePolygon(polyCoords));
        return new GeoMultiPolygon(polygons);
    }

    static Geometry ParsePoint(JsonElement coordinates)
        => new GeoPoint(coordinates[1].GetDouble(), coordinates[0].GetDouble()); // implicit GeoPoint → point geometry
}

record ParsedFeature(Geometry Geometry, Dictionary<string, JsonElement> Properties);
