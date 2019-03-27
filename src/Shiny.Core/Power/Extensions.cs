using System;


namespace Shiny.Power
{
    public static class Extensions
    {
        public static bool IsPluggedIn(this IPowerManager power) =>
            power.Status == PowerState.Charged ||
            power.Status == PowerState.Charging ||
            power.Status == PowerState.NoBattery;
    }
}
