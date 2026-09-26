using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using NativeGeocoder = Android.Locations.Geocoder;

namespace Shiny.Locations;


public class Geocoder(AndroidPlatform platform) : IGeocoder
{
    const int MaxResults = 5;

    public bool IsSupported => NativeGeocoder.IsPresent;


    public async Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default)
    {
        if (!NativeGeocoder.IsPresent)
            throw new InvalidOperationException("No geocoder is available on this device");

        var geocoder = new NativeGeocoder(platform.AppContext);
        IList<Address>? addresses;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var listener = new GeocodeListener();
            using var reg = cancelToken.Register(() => listener.Tcs.TrySetCanceled(cancelToken));
            geocoder.GetFromLocation(position.Latitude, position.Longitude, MaxResults, listener);
            addresses = await listener.Tcs.Task.ConfigureAwait(false);
        }
        else
        {
            // the pre-33 lookup blocks on network I/O - the binding's async wrapper moves it off the calling thread
            addresses = await geocoder
                .GetFromLocationAsync(position.Latitude, position.Longitude, MaxResults)
                .WaitAsync(cancelToken)
                .ConfigureAwait(false);
        }

        return (addresses ?? [])
            .Select(x => FromNative(x, position))
            .ToList();
    }


    static Placemark FromNative(Address address, Position position) => new(
        address.HasLatitude && address.HasLongitude
            ? new Position(address.Latitude, address.Longitude)
            : position,
        address.FeatureName,
        address.SubThoroughfare,
        address.Thoroughfare,
        address.SubLocality,
        address.Locality,
        address.SubAdminArea,
        address.AdminArea,
        address.PostalCode,
        address.CountryCode,
        address.CountryName,
        address.MaxAddressLineIndex >= 0 ? address.GetAddressLine(0) : null
    );


    class GeocodeListener : Java.Lang.Object, NativeGeocoder.IGeocodeListener
    {
        public TaskCompletionSource<IList<Address>?> Tcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnGeocode(IList<Address> addresses) => this.Tcs.TrySetResult(addresses);

        public void OnError(string? errorMessage) => this.Tcs.TrySetException(
            new InvalidOperationException("Geocoding failed - " + (errorMessage ?? "unknown error"))
        );
    }
}
