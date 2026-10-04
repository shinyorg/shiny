using System.Globalization;

namespace Sample.Shared.Maui.Pages.Locations;

[ShellMap<GeocodingPage>("geocoding")]
public partial class GeocodingViewModel(IGeocoder geocoder, IGpsManager gpsManager) : ObservableObject
{
    // defaults to a well-known address so the page works without a GPS fix
    [ObservableProperty] string latitude = "43.6426";
    [ObservableProperty] string longitude = "-79.3871";
    [ObservableProperty] string status = string.Empty;

    public bool IsSupported => geocoder.IsSupported;
    public ObservableCollection<PlacemarkItem> Placemarks { get; } = new();

    [RelayCommand]
    async Task UseCurrentPosition()
    {
        try
        {
            var access = await gpsManager.RequestAccess(GpsRequest.Foreground);
            if (access != AccessState.Available)
            {
                this.Status = $"GPS Access: {access}";
                return;
            }

            this.Status = "Getting position...";
            var reading = await gpsManager.GetCurrentPosition();
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
            // Android devices without a geocoding backend (typically no Google Play Services)
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
    public static PlacemarkItem From(Placemark p)
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
