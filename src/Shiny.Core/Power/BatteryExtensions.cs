using Shiny.Power;

namespace Shiny;


public static class BatteryExtensions
{
    /// <summary>
    /// Returns true if any battery state indicates the battery is plugged in
    /// </summary>
    public static bool IsPluggedIn(this IBattery battery) =>
        battery.Status == BatteryState.Full ||
        battery.Status == BatteryState.Charging ||
        battery.Status == BatteryState.None;
}
