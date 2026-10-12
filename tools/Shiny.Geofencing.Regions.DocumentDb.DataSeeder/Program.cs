using System.Globalization;
using System.IO.Compression;
using System.Text;
using Shiny.DocumentDb;
using Shiny.Geofencing.Regions.DocumentDb.DataSeeder;

// Regenerates the embedded city datasets (src/Shiny.Geofencing.Regions.DocumentDb/NorthAmerica/Data/UsCities.cs and
// CanadianCities.cs) from live authoritative sources. States/provinces are authored by hand (coarse
// boundaries) and are NOT regenerated here.
//
// Usage: dotnet run --project tools/Shiny.Geofencing.Regions.DocumentDb.DataSeeder [dataDir] [minPopulation]
//   dataDir       defaults to ../../src/Shiny.Geofencing.Regions.DocumentDb/NorthAmerica/Data relative to this project
//   minPopulation defaults to 100000 (US) / 25000 (CA) as in the shipped dataset

var dataDir = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Shiny.Geofencing.Regions.DocumentDb", "NorthAmerica", "Data");

dataDir = Path.GetFullPath(dataDir);
if (!Directory.Exists(dataDir))
    throw new DirectoryNotFoundException($"Data directory not found: {dataDir}");

Console.WriteLine($"Data directory: {dataDir}");

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
http.DefaultRequestHeaders.Add("User-Agent", "Shiny.Geofencing.Regions.DocumentDb.DataSeeder/1.0");

await RegenerateUsCitiesAsync(http, Path.Combine(dataDir, "UsCities.cs"));
await RegenerateCanadianCitiesAsync(http, Path.Combine(dataDir, "CanadianCities.cs"));

Console.WriteLine();
Console.WriteLine("Done. Review the git diff before committing.");

static async Task RegenerateUsCitiesAsync(HttpClient http, string outputPath)
{
    Console.WriteLine("Downloading US city boundaries from Census TIGERweb...");
    const string baseUrl = "https://tigerweb.geo.census.gov/arcgis/rest/services/Census2020/Places_CouSub_ConCity_SubMCD/MapServer/4/query";
    const int pageSize = 5000;

    var rows = new List<CityRow>();
    var offset = 0;
    while (true)
    {
        var url = $"{baseUrl}?where=POP100>=100000&outFields=BASENAME,STATE,POP100&outSR=4326"
            + $"&geometryPrecision=4&maxAllowableOffset=0.001&resultOffset={offset}&resultRecordCount={pageSize}&f=geojson";

        var json = await http.GetStringAsync(url);
        var features = GeoJsonParser.ParseFeatureCollection(json);
        if (features.Count == 0)
            break;

        foreach (var f in features)
        {
            var name = f.Properties["BASENAME"].GetString() ?? "";
            var state = FipsToStateAbbreviation(f.Properties["STATE"].GetString() ?? "");
            var pop = (long)f.Properties["POP100"].GetDouble();
            var c = f.Geometry.Centroid;
            rows.Add(new CityRow(name, state, pop, c.Longitude, c.Latitude));
        }

        Console.WriteLine($"  {rows.Count} US cities so far...");
        if (features.Count < pageSize)
            break;
        offset += pageSize;
    }

    WriteCityFile(outputPath, "UsCities", "CityData", "StateAbbreviation", rows);
    Console.WriteLine($"US Cities: {rows.Count} -> {Path.GetFileName(outputPath)}");
}

static async Task RegenerateCanadianCitiesAsync(HttpClient http, string outputPath)
{
    Console.WriteLine("Downloading Canadian population data from StatsCan...");
    var populationByCsd = await DownloadCanadianPopulationAsync(http);

    Console.WriteLine("Downloading Canadian city boundaries from StatsCan...");
    const string baseUrl = "https://geo.statcan.gc.ca/geo_wa/rest/services/2021/Cartographic_boundary_files/MapServer/9/query";
    const int pageSize = 2000;

    var rows = new List<CityRow>();
    var offset = 0;
    while (true)
    {
        var url = $"{baseUrl}?where=1=1&outFields=CSDUID,CSDNAME,PRUID&outSR=4326"
            + $"&geometryPrecision=4&maxAllowableOffset=0.001&resultOffset={offset}&resultRecordCount={pageSize}&f=geojson";

        var json = await http.GetStringAsync(url);
        var features = GeoJsonParser.ParseFeatureCollection(json);
        if (features.Count == 0)
            break;

        foreach (var f in features)
        {
            var csduid = f.Properties["CSDUID"].GetString() ?? "";
            if (populationByCsd.TryGetValue(csduid, out var pop) && pop.Population >= 25000)
            {
                var province = PruidToProvinceAbbreviation(f.Properties["PRUID"].GetString() ?? "");
                var c = f.Geometry.Centroid;
                rows.Add(new CityRow(pop.Name, province, pop.Population, c.Longitude, c.Latitude));
            }
        }

        Console.WriteLine($"  {rows.Count} Canadian cities matched...");
        if (features.Count < pageSize)
            break;
        offset += pageSize;
    }

    rows = rows.OrderByDescending(r => r.Population).ToList();
    WriteCityFile(outputPath, "CanadianCities", "CanadianCityData", "ProvinceAbbreviation", rows);
    Console.WriteLine($"Canadian Cities: {rows.Count} -> {Path.GetFileName(outputPath)}");
}

static async Task<Dictionary<string, (string Name, long Population)>> DownloadCanadianPopulationAsync(HttpClient http)
{
    const string csvUrl = "https://www150.statcan.gc.ca/n1/tbl/csv/98100002-eng.zip";
    var zipBytes = await http.GetByteArrayAsync(csvUrl);
    using var zipStream = new MemoryStream(zipBytes);
    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

    var csvEntry = archive.Entries
        .Where(e => e.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        .FirstOrDefault(e => !e.Name.Contains("MetaData", StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException("No data CSV found in StatsCan archive");

    using var reader = new StreamReader(csvEntry.Open());
    var headerLine = await reader.ReadLineAsync() ?? throw new InvalidOperationException("Empty CSV");
    if (headerLine.Length > 0 && headerLine[0] == '\uFEFF')
        headerLine = headerLine[1..];

    var headers = ParseCsvLine(headerLine);
    var geoIdx = FindColumnIndex(headers, "GEO");
    var dguidIdx = FindColumnIndex(headers, "DGUID");
    var popIdx = FindColumnIndex(headers, "Population, 2021");

    var result = new Dictionary<string, (string, long)>();
    while (await reader.ReadLineAsync() is { } line)
    {
        var fields = String.IsNullOrWhiteSpace(line) ? [] : ParseCsvLine(line);
        if (fields.Length > Math.Max(dguidIdx, popIdx))
        {
            var dguid = fields[dguidIdx];
            var marker = dguid.IndexOf("A0005", StringComparison.Ordinal);
            var csduid = marker < 0 ? "" : dguid[(marker + 5)..];

            if (csduid.Length > 0 && long.TryParse(fields[popIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var pop))
                result[csduid] = (fields[geoIdx], pop);
        }
    }

    Console.WriteLine($"  Parsed {result.Count} census subdivisions");
    return result;
}

static void WriteCityFile(string path, string className, string recordType, string regionProperty, List<CityRow> rows)
{
    var sb = new StringBuilder();
    sb.AppendLine("using System.Collections.Generic;");
    sb.AppendLine();
    sb.AppendLine("namespace Shiny.Locations.NorthAmerica.Data;");
    sb.AppendLine();
    sb.AppendLine($"static class {className}");
    sb.AppendLine("{");
    sb.AppendLine($"    public static IReadOnlyList<{recordType}> GetAll() => _cities;");
    sb.AppendLine();
    sb.AppendLine($"    private static readonly {recordType}[] _cities = new[]");
    sb.AppendLine("    {");
    foreach (var r in rows)
    {
        var name = r.Name.Replace("\\", "\\\\").Replace("\"", "\\\"");
        sb.AppendLine($"        new {recordType}(\"{name}\", \"{r.Region}\", {r.Population}, {r.Lon.ToString("F4", CultureInfo.InvariantCulture)}, {r.Lat.ToString("F4", CultureInfo.InvariantCulture)}),");
    }
    sb.AppendLine("    };");
    sb.AppendLine("}");
    sb.AppendLine();
    sb.AppendLine($"record {recordType}(string Name, string {regionProperty}, long Population, double Longitude, double Latitude);");

    File.WriteAllText(path, sb.ToString());
}

static string[] ParseCsvLine(string line)
{
    var fields = new List<string>();
    var inQuotes = false;
    var current = new StringBuilder();
    for (var i = 0; i < line.Length; i++)
    {
        var c = line[i];
        if (c == '"')
        {
            if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
            {
                current.Append('"');
                i++;
            }
            else
            {
                inQuotes = !inQuotes;
            }
        }
        else if (c == ',' && !inQuotes)
        {
            fields.Add(current.ToString());
            current.Clear();
        }
        else
        {
            current.Append(c);
        }
    }
    fields.Add(current.ToString());
    return fields.ToArray();
}

static int FindColumnIndex(string[] headers, string name)
{
    for (var i = 0; i < headers.Length; i++)
    {
        if (headers[i].Trim('"', ' ').Equals(name, StringComparison.OrdinalIgnoreCase))
            return i;
    }
    for (var i = 0; i < headers.Length; i++)
    {
        if (headers[i].Trim('"', ' ').Contains(name, StringComparison.OrdinalIgnoreCase))
            return i;
    }
    return -1;
}

static string FipsToStateAbbreviation(string fips) => fips switch
{
    "01" => "AL", "02" => "AK", "04" => "AZ", "05" => "AR", "06" => "CA",
    "08" => "CO", "09" => "CT", "10" => "DE", "11" => "DC", "12" => "FL",
    "13" => "GA", "15" => "HI", "16" => "ID", "17" => "IL", "18" => "IN",
    "19" => "IA", "20" => "KS", "21" => "KY", "22" => "LA", "23" => "ME",
    "24" => "MD", "25" => "MA", "26" => "MI", "27" => "MN", "28" => "MS",
    "29" => "MO", "30" => "MT", "31" => "NE", "32" => "NV", "33" => "NH",
    "34" => "NJ", "35" => "NM", "36" => "NY", "37" => "NC", "38" => "ND",
    "39" => "OH", "40" => "OK", "41" => "OR", "42" => "PA", "44" => "RI",
    "45" => "SC", "46" => "SD", "47" => "TN", "48" => "TX", "49" => "UT",
    "50" => "VT", "51" => "VA", "53" => "WA", "54" => "WV", "55" => "WI",
    "56" => "WY", "60" => "AS", "66" => "GU", "69" => "MP", "72" => "PR",
    "78" => "VI",
    _ => fips
};

static string PruidToProvinceAbbreviation(string pruid) => pruid switch
{
    "10" => "NL", "11" => "PE", "12" => "NS", "13" => "NB",
    "24" => "QC", "35" => "ON", "46" => "MB", "47" => "SK",
    "48" => "AB", "59" => "BC", "60" => "YT", "61" => "NT",
    "62" => "NU",
    _ => pruid
};

record CityRow(string Name, string Region, long Population, double Lon, double Lat);
