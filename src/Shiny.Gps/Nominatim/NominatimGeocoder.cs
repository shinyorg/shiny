using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


/// <summary>
/// A managed <see cref="IGeocoder"/> over the OpenStreetMap Nominatim API. <c>AddGeocoding()</c> registers it on
/// every platform without a native geocoder (Windows, Linux, macOS, Blazor WebAssembly) and on Android devices
/// without a geocoding backend. It keeps to the public server's usage policy: one request at a time, at most one
/// per <see cref="NominatimOptions.MinimumRequestInterval"/>, with results cached.
/// </summary>
/// <remarks>
/// Results are © OpenStreetMap contributors (ODbL) - show <see cref="Attribution"/> wherever you display them.
/// </remarks>
public class NominatimGeocoder : IGeocoder, IDisposable
{
    /// <summary>
    /// The attribution OpenStreetMap requires wherever Nominatim results are displayed.
    /// </summary>
    public const string Attribution = "© OpenStreetMap contributors";

    readonly NominatimOptions options;
    readonly HttpClient httpClient;
    readonly bool ownsClient;
    readonly TimeProvider timeProvider;
    readonly SemaphoreSlim gate = new(1, 1);
    readonly Dictionary<(long, long), IReadOnlyList<Placemark>> cache = new();
    readonly Queue<(long, long)> cacheOrder = new();
    long lastRequestTimestamp;
    bool hasRequested;


    /// <summary>
    /// Creates a geocoder with its own <see cref="HttpClient"/>.
    /// </summary>
    public NominatimGeocoder(NominatimOptions? options = null)
        : this(options ?? new NominatimOptions(), new HttpClient(), TimeProvider.System, true) { }


    /// <summary>
    /// Creates a geocoder that sends its requests through <paramref name="httpClient"/>, which the caller owns.
    /// </summary>
    public NominatimGeocoder(NominatimOptions options, HttpClient httpClient, TimeProvider? timeProvider = null)
        : this(options, httpClient, timeProvider ?? TimeProvider.System, false) { }


    NominatimGeocoder(NominatimOptions options, HttpClient httpClient, TimeProvider timeProvider, bool ownsClient)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.timeProvider = timeProvider;
        this.ownsClient = ownsClient;
    }


    /// <summary>
    /// Always true - the lookup only needs network access.
    /// </summary>
    public bool IsSupported => true;


    public async Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(position);

        // ~1.1m at the equator - close enough that two readings of the same spot share a cache entry
        var key = ((long)Math.Round(position.Latitude * 100_000), (long)Math.Round(position.Longitude * 100_000));

        await this.gate.WaitAsync(cancelToken).ConfigureAwait(false);
        try
        {
            if (this.cache.TryGetValue(key, out var cached))
                return cached;

            await this.WaitForRateLimit(cancelToken).ConfigureAwait(false);

            NominatimResponse? response;
            try
            {
                using var request = this.CreateRequest(position);
                using var httpResponse = await this.httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken)
                    .ConfigureAwait(false);

                httpResponse.EnsureSuccessStatusCode();
                response = await httpResponse.Content
                    .ReadFromJsonAsync(NominatimJsonContext.Default.NominatimResponse, cancelToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                // a failed request still counts against the server's limit
                this.lastRequestTimestamp = this.timeProvider.GetTimestamp();
                this.hasRequested = true;
            }

            IReadOnlyList<Placemark> result = response == null || response.Error != null
                ? []
                : [ToPlacemark(response, position)];

            this.AddToCache(key, result);
            return result;
        }
        finally
        {
            this.gate.Release();
        }
    }


    async Task WaitForRateLimit(CancellationToken cancelToken)
    {
        if (!this.hasRequested)
            return;

        var elapsed = this.timeProvider.GetElapsedTime(this.lastRequestTimestamp);
        var wait = this.options.MinimumRequestInterval - elapsed;
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, this.timeProvider, cancelToken).ConfigureAwait(false);
    }


    HttpRequestMessage CreateRequest(Position position)
    {
        // language and email go on the query string rather than as headers - a custom header would make a browser
        // send a CORS preflight, and these are both documented query parameters
        var query = String.Create(
            CultureInfo.InvariantCulture,
            $"reverse?format=jsonv2&addressdetails=1&lat={position.Latitude:R}&lon={position.Longitude:R}"
        );
        if (!String.IsNullOrWhiteSpace(this.options.Language))
            query += "&accept-language=" + Uri.EscapeDataString(this.options.Language);

        if (!String.IsNullOrWhiteSpace(this.options.Email))
            query += "&email=" + Uri.EscapeDataString(this.options.Email);

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(EnsureTrailingSlash(this.options.BaseUri), query));

        // browsers forbid setting User-Agent - the Referer they send identifies the app instead
        if (!OperatingSystem.IsBrowser() && !String.IsNullOrWhiteSpace(this.options.UserAgent))
            request.Headers.TryAddWithoutValidation("User-Agent", this.options.UserAgent);

        return request;
    }


    void AddToCache((long, long) key, IReadOnlyList<Placemark> result)
    {
        if (this.options.CacheSize <= 0)
            return;

        while (this.cacheOrder.Count >= this.options.CacheSize)
            this.cache.Remove(this.cacheOrder.Dequeue());

        this.cache[key] = result;
        this.cacheOrder.Enqueue(key);
    }


    static Uri EnsureTrailingSlash(Uri uri)
        => uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");


    internal static Placemark ToPlacemark(NominatimResponse response, Position requested)
    {
        var a = response.Address;
        var position = TryParse(response.Latitude, out var lat) && TryParse(response.Longitude, out var lng)
            ? new Position(lat, lng)
            : requested;

        return new Placemark(
            position,
            Empty(response.Name),
            Empty(a?.HouseNumber),
            Empty(a?.Road) ?? Empty(a?.Pedestrian),
            Empty(a?.Suburb) ?? Empty(a?.Neighbourhood) ?? Empty(a?.Quarter) ?? Empty(a?.CityDistrict),
            Empty(a?.City) ?? Empty(a?.Town) ?? Empty(a?.Village) ?? Empty(a?.Hamlet) ?? Empty(a?.Municipality),
            Empty(a?.County) ?? Empty(a?.StateDistrict),
            Empty(a?.State) ?? Empty(a?.Province) ?? Empty(a?.Region),
            Empty(a?.Postcode),
            Empty(a?.CountryCode)?.ToUpperInvariant(),
            Empty(a?.Country),
            Empty(response.DisplayName)
        );
    }


    static bool TryParse(string? value, out double result)
        => Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

    static string? Empty(string? value) => String.IsNullOrWhiteSpace(value) ? null : value;


    public void Dispose()
    {
        if (this.ownsClient)
            this.httpClient.Dispose();

        this.gate.Dispose();
        GC.SuppressFinalize(this);
    }
}
