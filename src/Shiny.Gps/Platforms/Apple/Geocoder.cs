using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Contacts;
using CoreLocation;
using Foundation;
using MapKit;

namespace Shiny.Locations;


public class Geocoder : IGeocoder
{
    // kCLErrorGeocodeFoundNoResult - a lookup that simply matched nothing
    const string CLErrorDomain = "kCLErrorDomain";
    const long GeocodeFoundNoResult = 8;

    public bool IsSupported => true;


    public async Task<IReadOnlyList<Placemark>> ReverseGeocode(Position position, CancellationToken cancelToken = default)
    {
        var location = new CLLocation(position.Latitude, position.Longitude);
        try
        {
            // CLGeocoder is deprecated from iOS 26 in favour of MapKit's MKReverseGeocodingRequest
            if (OperatingSystem.IsIOSVersionAtLeast(26) || OperatingSystem.IsMacCatalystVersionAtLeast(26))
                return await ReverseWithMapKit(location, position, cancelToken).ConfigureAwait(false);

            return await ReverseWithCoreLocation(location, position, cancelToken).ConfigureAwait(false);
        }
        catch (NSErrorException ex) when (ex.Error.Domain == CLErrorDomain && ex.Error.Code == GeocodeFoundNoResult)
        {
            return [];
        }
    }


    [System.Runtime.Versioning.SupportedOSPlatform("ios26.0")]
    [System.Runtime.Versioning.SupportedOSPlatform("maccatalyst26.0")]
    static async Task<IReadOnlyList<Placemark>> ReverseWithMapKit(CLLocation location, Position position, CancellationToken cancelToken)
    {
        using var request = new MKReverseGeocodingRequest(location);
        using var reg = cancelToken.Register(request.Cancel);

        var items = await request.GetMapItemsAsync().ConfigureAwait(false);
        cancelToken.ThrowIfCancellationRequested();

        return (items ?? [])
            .Select(x => FromNative(x.Placemark, position, x.Name, x.Address?.FullAddress))
            .ToList();
    }


    [System.Runtime.Versioning.ObsoletedOSPlatform("ios26.0")]
    [System.Runtime.Versioning.ObsoletedOSPlatform("maccatalyst26.0")]
    static async Task<IReadOnlyList<Placemark>> ReverseWithCoreLocation(CLLocation location, Position position, CancellationToken cancelToken)
    {
        var geocoder = new CLGeocoder();
        using var reg = cancelToken.Register(geocoder.CancelGeocode);

        var placemarks = await geocoder.ReverseGeocodeLocationAsync(location).ConfigureAwait(false);
        cancelToken.ThrowIfCancellationRequested();

        return (placemarks ?? [])
            .Select(x => FromNative(x, position, x.Name, Format(x.PostalAddress)))
            .ToList();
    }


    [System.Runtime.Versioning.ObsoletedOSPlatform("ios26.0")]
    [System.Runtime.Versioning.ObsoletedOSPlatform("maccatalyst26.0")]
    static string? Format(CNPostalAddress? address)
    {
        if (address == null)
            return null;

        var lines = CNPostalAddressFormatter
            .GetStringFrom(address, CNPostalAddressFormatterStyle.MailingAddress)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.Length == 0 ? null : String.Join(", ", lines);
    }


    static Placemark FromNative(CLPlacemark? native, Position position, string? name, string? formatted) => new(
        native?.Location?.Coordinate.FromNative() ?? position,
        name,
        native?.SubThoroughfare,
        native?.Thoroughfare,
        native?.SubLocality,
        native?.Locality,
        native?.SubAdministrativeArea,
        native?.AdministrativeArea,
        native?.PostalCode,
        native?.IsoCountryCode,
        native?.Country,
        formatted
    );
}
