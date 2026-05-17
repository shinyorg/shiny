using System;
using System.Reactive.Linq;
using Shiny.Power;

namespace Shiny;


/// <summary>
/// Convenience extensions over <see cref="IBattery"/>.
/// </summary>
public static class BatteryExtensions
{
    /// <summary>
    /// Emits true when the battery transitions to a charging state and false when it stops charging.
    /// </summary>
    /// <param name="battery">The battery service.</param>
    public static IObservable<bool> WhenChargingChanged(this IBattery battery)
        => battery.WhenChanged().Select(x =>
            x.Status == BatteryState.Full ||
            x.Status == BatteryState.Charging
        );

    /// <summary>
    /// Returns true if the device is currently plugged in (full, charging, or has no battery).
    /// </summary>
    /// <param name="battery">The battery service.</param>
    public static bool IsPluggedIn(this IBattery battery) =>
        battery.Status == BatteryState.Full ||
        battery.Status == BatteryState.Charging ||
        battery.Status == BatteryState.None;
}
