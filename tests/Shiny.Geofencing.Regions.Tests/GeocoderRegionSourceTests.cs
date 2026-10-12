using Shiny.Extensions.Stores;
using Shiny.Locations;
using Xunit;

namespace Shiny.Geofencing.Regions.Tests;

public class GeocoderRegionSourceTests
{
    static readonly Position Denver = new(39.7392, -104.9903);
    static readonly Position Boulder = new(40.0150, -105.2705);

    readonly FakeGeocoder geocoder = new();
    readonly MemoryKeyValueStore state = new();
    readonly RecordingDelegate handler = new();


    public GeocoderRegionSourceTests()
    {
        this.geocoder.Results[Denver] = [Place(Denver, "Denver")];
        this.geocoder.Results[Boulder] = [Place(Boulder, "Boulder")];
    }


    RegionEvaluator Evaluator() => Geo.Evaluator(
        null,
        this.state,
        this.handler,
        new RegionGeofenceConfig()
            .AddRegionSet("states", new GeocoderRegionSource(this.geocoder, GeocoderRegionLevel.AdministrativeArea))
            .AddRegionSet("cities", new GeocoderRegionSource(this.geocoder, GeocoderRegionLevel.Locality))
    );


    [Theory]
    [InlineData(GeocoderRegionLevel.Country, "US", "United States")]
    [InlineData(GeocoderRegionLevel.AdministrativeArea, "US-Colorado", "Colorado")]
    [InlineData(GeocoderRegionLevel.SubAdministrativeArea, "US-Colorado-Denver County", "Denver County")]
    [InlineData(GeocoderRegionLevel.Locality, "US-Colorado-Denver", "Denver")]
    [InlineData(GeocoderRegionLevel.SubLocality, "US-Colorado-Denver-LoDo", "LoDo")]
    [InlineData(GeocoderRegionLevel.PostalCode, "US-80202", "80202")]
    public async Task Levels_Build_Stable_Ids(GeocoderRegionLevel level, string id, string name)
    {
        var match = await new GeocoderRegionSource(this.geocoder, level).FindRegion(Denver);

        Assert.NotNull(match);
        Assert.Equal(id, match.Id);
        Assert.Equal(name, match.Name);
        Assert.IsType<Placemark>(match.Region);
    }


    [Fact]
    public async Task A_Missing_Address_Part_Is_Outside()
    {
        this.geocoder.Results[Denver] = [Place(Denver, null)];

        var match = await new GeocoderRegionSource(this.geocoder, GeocoderRegionLevel.Locality).FindRegion(Denver);

        Assert.Null(match);
    }


    [Fact]
    public async Task Moving_Between_Cities_Exits_Then_Enters_And_Keeps_The_State()
    {
        var evaluator = this.Evaluator();

        var arrival = await evaluator.Evaluate(Denver);
        Assert.Equal(2, arrival.Count);

        var moved = await evaluator.Evaluate(Boulder);

        Assert.Equal(2, moved.Count);
        Assert.False(moved[0].Entered);
        Assert.Equal("US-Colorado-Denver", moved[0].RegionId);
        Assert.True(moved[1].Entered);
        Assert.Equal("Boulder", moved[1].RegionAs<Placemark>()!.Locality);
    }


    [Fact]
    public async Task Sets_Share_One_Lookup_Per_Reading()
    {
        await this.Evaluator().Evaluate(Denver);

        Assert.Equal(1, this.geocoder.Calls);
    }


    [Fact]
    public async Task A_Failed_Lookup_Does_Not_Raise_A_False_Exit()
    {
        var evaluator = this.Evaluator();
        await evaluator.Evaluate(Denver);

        this.geocoder.Offline = true;
        var changes = await evaluator.Evaluate(Boulder);

        Assert.Empty(changes);

        // back online - the failed lookup was not cached, and the device is still "in" Denver until now
        this.geocoder.Offline = false;
        var moved = await evaluator.Evaluate(Boulder);
        Assert.Contains(moved, c => c is { RegionSet: "cities", Entered: false, RegionId: "US-Colorado-Denver" });
    }


    [Fact]
    public async Task An_Exit_After_A_Restart_Carries_Only_The_Id()
    {
        await this.Evaluator().Evaluate(Denver);

        var changes = await this.Evaluator().Evaluate(Boulder);

        var exit = Assert.Single(changes, c => c is { RegionSet: "cities", Entered: false });
        Assert.Equal("US-Colorado-Denver", exit.RegionId);
        Assert.Null(exit.Region);
    }


    static Placemark Place(Position position, string? city) => new(
        position,
        Name: null,
        SubThoroughfare: null,
        Thoroughfare: null,
        SubLocality: city == null ? null : "LoDo",
        Locality: city,
        SubAdministrativeArea: city == null ? null : $"{city} County",
        AdministrativeArea: "Colorado",
        PostalCode: "80202",
        CountryCode: "US",
        CountryName: "United States",
        FormattedAddress: null
    );


    class FakeGeocoder : IGeocoder
    {
        public Dictionary<Position, IReadOnlyList<Placemark>> Results { get; } = new();
        public bool Offline { get; set; }
        public int Calls { get; private set; }
        public bool IsSupported => true;

        public Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default)
        {
            this.Calls++;
            if (this.Offline)
                return Task.FromException<IReadOnlyList<Placemark>>(new HttpRequestException("offline"));

            return Task.FromResult(this.Results.TryGetValue(position, out var r) ? r : (IReadOnlyList<Placemark>)[]);
        }
    }
}
