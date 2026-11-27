using Windows.Devices.Enumeration;

namespace Shiny.Locations;


static class WindowsLocationAccess
{
    /// <summary>
    /// The location permission as it stands, without prompting - unlike <c>Geolocator.RequestAccessAsync</c>.
    /// </summary>
    public static AccessState GetCurrentStatus()
    {
        var info = DeviceAccessInformation.CreateFromDeviceClass(DeviceClass.Location);
        return info.CurrentStatus switch
        {
            DeviceAccessStatus.Allowed => AccessState.Available,
            DeviceAccessStatus.DeniedByUser => AccessState.Denied,
            // location is switched off for the whole device, or by policy
            DeviceAccessStatus.DeniedBySystem => AccessState.Disabled,
            _ => AccessState.Unknown
        };
    }
}
