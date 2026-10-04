using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shiny.Locations;
using Xunit;

namespace Shiny.Locations.Tests;


public class NominatimGeocoderTests
{
    const string CnTowerJson = """
    {
        "place_id": 123,
        "licence": "Data © OpenStreetMap contributors, ODbL 1.0. http://osm.org/copyright",
        "lat": "43.6425637",
        "lon": "-79.3870871",
        "category": "tourism",
        "type": "attraction",
        "name": "CN Tower",
        "display_name": "CN Tower, 290, Bremner Boulevard, Entertainment District, Spadina—Fort York, Old Toronto, Toronto, Golden Horseshoe, Ontario, M5V 3L9, Canada",
        "address": {
            "tourism": "CN Tower",
            "house_number": "290",
            "road": "Bremner Boulevard",
            "neighbourhood": "Entertainment District",
            "quarter": "Spadina—Fort York",
            "city": "Toronto",
            "state_district": "Golden Horseshoe",
            "state": "Ontario",
            "ISO3166-2-lvl4": "CA-ON",
            "postcode": "M5V 3L9",
            "country": "Canada",
            "country_code": "ca"
        },
        "boundingbox": ["43.6423", "43.6428", "-79.3874", "-79.3868"]
    }
    """;

    static readonly Position CnTower = new(43.6426, -79.3871);


    [Fact]
    public async Task ReverseGeocode_MapsAddress()
    {
        var (geocoder, handler, _) = Create();
        handler.Respond(CnTowerJson);

        var result = await geocoder.ReverseGeocode(CnTower);

        var p = Assert.Single(result);
        Assert.Equal(43.6425637, p.Position.Latitude, 6);
        Assert.Equal(-79.3870871, p.Position.Longitude, 6);
        Assert.Equal("CN Tower", p.Name);
        Assert.Equal("290", p.SubThoroughfare);
        Assert.Equal("Bremner Boulevard", p.Thoroughfare);
        Assert.Equal("Entertainment District", p.SubLocality);
        Assert.Equal("Toronto", p.Locality);
        Assert.Equal("Golden Horseshoe", p.SubAdministrativeArea);
        Assert.Equal("Ontario", p.AdministrativeArea);
        Assert.Equal("M5V 3L9", p.PostalCode);
        Assert.Equal("CA", p.CountryCode);
        Assert.Equal("Canada", p.CountryName);
        Assert.StartsWith("CN Tower, 290, Bremner Boulevard", p.FormattedAddress);
    }


    [Fact]
    public async Task ReverseGeocode_FallsBackAcrossLocalityKeys()
    {
        var (geocoder, handler, _) = Create();
        handler.Respond("""
        {
            "lat": "45.0", "lon": "-75.0", "name": "", "display_name": "Main Street, Smalltown, Ontario, Canada",
            "address": { "road": "Main Street", "town": "Smalltown", "county": "Some County", "province": "Ontario", "country_code": "ca" }
        }
        """);

        var p = Assert.Single(await geocoder.ReverseGeocode(new Position(45, -75)));
        Assert.Null(p.Name);
        Assert.Null(p.SubThoroughfare);
        Assert.Equal("Smalltown", p.Locality);
        Assert.Equal("Some County", p.SubAdministrativeArea);
        Assert.Equal("Ontario", p.AdministrativeArea);
    }


    [Fact]
    public async Task ReverseGeocode_NoMatch_ReturnsEmpty()
    {
        // the middle of the ocean - Nominatim answers 200 with only "error"
        var (geocoder, handler, _) = Create();
        handler.Respond("""{ "error": "Unable to geocode" }""");

        var result = await geocoder.ReverseGeocode(new Position(0, -30));
        Assert.Empty(result);
    }


    [Fact]
    public async Task ReverseGeocode_SendsPolicyCompliantRequest()
    {
        var (geocoder, handler, _) = Create(o =>
        {
            o.BaseUri = new Uri("https://geo.example.com/nominatim");
            o.UserAgent = "MyApp/2.0 (Shiny.Gps)";
            o.Email = "dev@example.com";
            o.Language = "fr-CA";
        });
        handler.Respond(CnTowerJson);

        await geocoder.ReverseGeocode(CnTower);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://geo.example.com/nominatim/reverse", request.RequestUri!.GetLeftPart(UriPartial.Path));

        var query = request.RequestUri.Query;
        Assert.Contains("format=jsonv2", query);
        Assert.Contains("addressdetails=1", query);
        Assert.Contains("lat=43.6426", query);
        Assert.Contains("lon=-79.3871", query);
        Assert.Contains("accept-language=fr-CA", query);
        Assert.Contains("email=dev%40example.com", query);
        Assert.Equal("MyApp/2.0 (Shiny.Gps)", String.Join(" ", request.Headers.GetValues("User-Agent")));
    }


    [Fact]
    public async Task ReverseGeocode_FormatsCoordinatesInvariantly()
    {
        var culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            // a comma decimal separator must never reach the query string
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var (geocoder, handler, _) = Create();
            handler.Respond(CnTowerJson);

            await geocoder.ReverseGeocode(CnTower);
            Assert.Contains("lat=43.6426&", handler.Requests[0].RequestUri!.Query);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }


    [Fact]
    public async Task ReverseGeocode_CachesNearbyLookups()
    {
        var (geocoder, handler, _) = Create();
        handler.Respond(CnTowerJson);

        await geocoder.ReverseGeocode(CnTower);
        // a few centimetres away rounds to the same cache entry
        var second = await geocoder.ReverseGeocode(new Position(43.6426001, -79.3871001));

        Assert.Single(handler.Requests);
        Assert.Single(second);
    }


    [Fact]
    public async Task ReverseGeocode_CacheDisabled_AlwaysRequests()
    {
        var (geocoder, handler, time) = Create(o => o.CacheSize = 0);
        handler.Respond(CnTowerJson);

        await geocoder.ReverseGeocode(CnTower);
        time.Advance(TimeSpan.FromSeconds(1));
        await geocoder.ReverseGeocode(CnTower);

        Assert.Equal(2, handler.Requests.Count);
    }


    [Fact]
    public async Task ReverseGeocode_WaitsForMinimumInterval()
    {
        var (geocoder, handler, time) = Create();
        handler.Respond(CnTowerJson);

        await geocoder.ReverseGeocode(CnTower);

        var second = geocoder.ReverseGeocode(new Position(10, 10));
        await Task.Delay(50);
        Assert.False(second.IsCompleted);
        Assert.Single(handler.Requests);

        time.Advance(TimeSpan.FromSeconds(1));
        await second.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, handler.Requests.Count);
    }


    [Fact]
    public async Task ReverseGeocode_ServerRefusal_Throws()
    {
        var (geocoder, handler, _) = Create();
        handler.Respond("", HttpStatusCode.TooManyRequests);

        await Assert.ThrowsAsync<HttpRequestException>(() => geocoder.ReverseGeocode(CnTower));
    }


    [Fact]
    public async Task ReverseGeocode_Cancelled_Throws()
    {
        var (geocoder, handler, _) = Create();
        handler.Respond(CnTowerJson);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => geocoder.ReverseGeocode(CnTower, cts.Token));
        Assert.Empty(handler.Requests);
    }


    [Fact]
    public void AddGeocoding_RegistersNominatimWithoutNativeGeocoder()
    {
        var services = new ServiceCollection();
        services.AddGeocoding(o => o.Email = "dev@example.com");

        using var sp = services.BuildServiceProvider();
        var geocoder = sp.GetRequiredService<IGeocoder>();
        Assert.IsType<NominatimGeocoder>(geocoder);
        Assert.True(geocoder.IsSupported);
    }


    [Fact]
    public void AddGeocoding_KeepsExistingRegistration()
    {
        var existing = new StubGeocoder();
        var services = new ServiceCollection();
        services.AddSingleton<IGeocoder>(existing);
        services.AddGeocoding();

        using var sp = services.BuildServiceProvider();
        Assert.Same(existing, sp.GetRequiredService<IGeocoder>());
    }


    class StubGeocoder : IGeocoder
    {
        public bool IsSupported => true;
        public Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default)
            => Task.FromResult<IReadOnlyList<Placemark>>([]);
    }


    static (NominatimGeocoder Geocoder, FakeHandler Handler, FakeTimeProvider Time) Create(Action<NominatimOptions>? configure = null)
    {
        var options = new NominatimOptions();
        configure?.Invoke(options);

        var handler = new FakeHandler();
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        return (new NominatimGeocoder(options, new HttpClient(handler), time), handler, time);
    }


    class FakeHandler : HttpMessageHandler
    {
        string body = "{}";
        HttpStatusCode status = HttpStatusCode.OK;

        public List<HttpRequestMessage> Requests { get; } = new();

        public void Respond(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            this.body = body;
            this.status = status;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (this.Requests)
                this.Requests.Add(request);

            return Task.FromResult(new HttpResponseMessage(this.status)
            {
                Content = new StringContent(this.body, Encoding.UTF8, "application/json")
            });
        }
    }
}
