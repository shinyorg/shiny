using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<GeocodingPage>("geocoding")]
public partial class GeocodingViewModel(IGeocoder geocoder, IServiceProvider services) : ObservableObject
{
    // GPS is only registered on iOS, Mac Catalyst & Android - elsewhere the position is typed in
    readonly IGpsManager? gpsManager = services.GetService<IGpsManager>();

    // defaults to a well-known address so the page works without a GPS fix
    [ObservableProperty] string latitude = "43.6426";
    [ObservableProperty] string longitude = "-79.3871";
    [ObservableProperty] string status = string.Empty;

    public bool IsSupported => geocoder.IsSupported;
    public bool HasGps => this.gpsManager != null;
    public string Provider => geocoder is NominatimGeocoder ? "OpenStreetMap Nominatim" : "Native";

    // OpenStreetMap requires attribution wherever Nominatim results are shown
    public string? Attribution => geocoder is NominatimGeocoder ? NominatimGeocoder.Attribution : null;
    public bool ShowAttribution => this.Attribution != null;
    public ObservableCollection<PlacemarkItem> Placemarks { get; } = new();

    [RelayCommand]
    async Task UseCurrentPosition()
    {
        if (this.gpsManager == null)
            return;

        try
        {
            var access = await this.gpsManager.RequestAccess(GpsRequest.Foreground);
            if (access != AccessState.Available)
            {
                this.Status = $"GPS Access: {access}";
                return;
            }

            this.Status = "Getting position...";
            var reading = await this.gpsManager.GetCurrentPosition();
            if (reading == null)
            {
                this.Status = "No position available";
                return;
            }
            this.Latitude = reading.Position.Latitude.ToString("F6", CultureInfo.InvariantCulture);
            this.Longitude = reading.Position.Longitude.ToString("F6", CultureInfo.InvariantCulture);
            this.Status = "Position received";
        }
        catch (Exception ex)
        {
            this.Status = "Error: " + ex.Message;
        }
    }

    [RelayCommand]
    async Task ReverseGeocode()
    {
        if (!geocoder.IsSupported)
        {
            this.Status = "No geocoder on this device";
            return;
        }
        if (!double.TryParse(this.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) || lat is < -90 or > 90 ||
            !double.TryParse(this.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng) || lng is < -180 or > 180)
        {
            this.Status = "Enter a valid latitude & longitude";
            return;
        }

        try
        {
            this.Status = "Looking up...";
            this.Placemarks.Clear();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var results = await geocoder.ReverseGeocode(new Position(lat, lng), cts.Token);

            foreach (var p in results)
                this.Placemarks.Add(PlacemarkItem.From(p));

            this.Status = results.Count == 0 ? "No placemarks found" : $"{results.Count} placemark(s)";
        }
        catch (Exception ex)
        {
            this.Status = "Error: " + ex.Message;
        }
    }
}

public record PlacemarkItem(string Title, string Description)
{
    public static PlacemarkItem From(Shiny.Locations.Placemark p)
    {
        var street = Join(" ", p.SubThoroughfare, p.Thoroughfare);
        var title = p.FormattedAddress ?? p.Name ?? street ?? "(unnamed)";
        var description = Join(", ", p.Name == title ? null : p.Name, p.SubLocality, p.Locality, p.AdministrativeArea, p.PostalCode, p.CountryCode);
        return new(title, description ?? string.Empty);
    }

    static string? Join(string separator, params string?[] parts)
    {
        var value = String.Join(separator, parts.Where(x => !String.IsNullOrWhiteSpace(x)));
        return value.Length == 0 ? null : value;
    }
}
