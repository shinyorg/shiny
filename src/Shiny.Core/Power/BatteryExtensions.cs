using System;
using System.Reactive.Linq;
using Shiny.Power;

namespace Shiny;


public static class BatteryExtensions
{
    /// <summary>
    /// Detects when charging state has changed
    /// </summary>
    /// <param name="battery"></param>
    /// <returns></returns>
    public static IObservable<bool> WhenChargingChanged(this IBattery battery)
        => battery.WhenChanged().Select(x =>
            x.Status == BatteryState.Full ||
            x.Status == BatteryState.Charging
        );

    /// <summary>
    /// Returns true if any battery state indicates the battery is plugged in
    /// </summary>
    /// <param name="battery"></param>
    /// <returns></returns>
    public static bool IsPluggedIn(this IBattery battery) =>
        battery.Status == BatteryState.Full ||
        battery.Status == BatteryState.Charging ||
        battery.Status == BatteryState.None;
}
