using Shiny.Net;
using Shiny.Power;

namespace Shiny.Jobs.Infrastructure;


/// <summary>
/// The foreground constraint checks shared by the in-process job runners (<c>JobLifecycleTask</c> on
/// Android/Apple and the plain .NET <c>JobManager</c>).
/// </summary>
static class JobConstraints
{
    /// <summary>
    /// <see cref="IBattery.Level"/> is 0.0 - 1.0, so "low" is anything at or below 20%.
    /// </summary>
    internal const double LowBatteryLevel = 0.2;


    /// <summary>
    /// A null battery means the host has no battery API (plain .NET on Windows / macOS) - treated as
    /// mains powered, which is what such a machine almost always is.
    /// </summary>
    internal static bool IsOnExternalPower(IBattery? battery)
        => battery == null || battery.IsPluggedIn();


    internal static bool HasPowerLevel(JobRegistration reg, IBattery? battery)
    {
        if (!reg.BatteryNotLow || battery == null)
            return true;

        return battery.Level > LowBatteryLevel || battery.IsPluggedIn();
    }


    internal static bool HasChargeStatus(JobRegistration reg, IBattery? battery)
        => !reg.DeviceCharging || IsOnExternalPower(battery);


    /// <summary>
    /// <see cref="IConnectivity"/> has no "metered" flag, so unmetered means internet that is neither
    /// constrained (roaming / Low Data Mode) nor over a cellular transport.
    /// </summary>
    internal static bool HasReqInternet(JobRegistration reg, IConnectivity connectivity) => reg.RequiredInternetAccess switch
    {
        InternetAccess.Any => connectivity.IsInternetAvailable(true),
        InternetAccess.Unmetered => connectivity.IsInternetAvailable(false) && !connectivity.ConnectionTypes.HasFlag(ConnectionTypes.Cellular),
        InternetAccess.None => true,
        _ => false
    };
}
