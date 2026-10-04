using System;
using System.Reflection;

namespace Shiny.Locations;


/// <summary>
/// Options for the OpenStreetMap Nominatim geocoder - the <see cref="IGeocoder"/> <c>AddGeocoding()</c> registers on
/// platforms without a native one (Windows, Linux, macOS, Blazor WebAssembly) and on Android devices without a
/// geocoding backend.
/// </summary>
/// <remarks>
/// The public server at nominatim.openstreetmap.org is free, but it has a usage policy
/// (https://operations.osmfoundation.org/policies/nominatim/): at most one request per second, an identifying
/// User-Agent, results cached, and "© OpenStreetMap contributors" shown wherever results are displayed. The
/// defaults here keep to it. For heavy use, point <see cref="BaseUri"/> at your own Nominatim server.
/// </remarks>
public class NominatimOptions
{
    /// <summary>
    /// The attribution OpenStreetMap requires wherever Nominatim results are displayed - ideally linked to
    /// https://www.openstreetmap.org/copyright.
    /// </summary>
    public const string Attribution = "© OpenStreetMap contributors";

    /// <summary>
    /// The public OpenStreetMap Nominatim server.
    /// </summary>
    public static readonly Uri PublicServer = new("https://nominatim.openstreetmap.org/");

    /// <summary>
    /// The Nominatim server to query. Defaults to <see cref="PublicServer"/>; set it to a self-hosted
    /// (or any Nominatim API compatible) server to lift the public rate limit.
    /// </summary>
    public Uri BaseUri { get; set; } = PublicServer;

    /// <summary>
    /// The User-Agent sent with every request. The public server's policy requires one that identifies your app -
    /// it defaults to your entry assembly's name and version. Browsers do not let an app set this header, so it
    /// is not sent from Blazor WebAssembly (the page's Referer identifies the app there instead).
    /// </summary>
    public string UserAgent { get; set; } = DefaultUserAgent();

    /// <summary>
    /// An optional contact email sent with each request - the public server asks for one if you make a large
    /// number of requests, so it can reach you before blocking.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The language results are returned in, as an Accept-Language value (e.g. "en", "fr-CA"). Defaults to the
    /// current UI culture; null leaves it to the server (local names).
    /// </summary>
    public string? Language { get; set; } = String.IsNullOrEmpty(System.Globalization.CultureInfo.CurrentUICulture.Name)
        ? null
        : System.Globalization.CultureInfo.CurrentUICulture.Name;

    /// <summary>
    /// The minimum time between two requests to the server. Defaults to the public server's limit of one second;
    /// lower it only for a server you run yourself.
    /// </summary>
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How many results are kept in memory, keyed on the position rounded to about a metre. 0 disables the cache.
    /// </summary>
    public int CacheSize { get; set; } = 100;


    static string DefaultUserAgent()
    {
        var name = Assembly.GetEntryAssembly()?.GetName();
        var app = String.IsNullOrWhiteSpace(name?.Name) ? "ShinyApp" : name!.Name;
        var version = name?.Version?.ToString() ?? "1.0";
        return $"{app}/{version} (Shiny.Gps)";
    }
}
