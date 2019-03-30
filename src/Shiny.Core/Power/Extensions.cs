using System;


namespace Shiny.Power
{
    public static class Extensions
    {
        /// <summary>
        /// Sets up an easy power status observable
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static IObservable<PowerState> WhenStatusChanged(this IPowerManager power)
            => power.WhenAnyProperty(x => x.Status);


        /// <summary>
        /// Sets up an easy battery level observable
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static IObservable<int> WhenBatteryLevelChanged(this IPowerManager power)
            => power.WhenAnyProperty(x => x.BatteryLevel);


        /// <summary>
        /// Returns true if any power state indicates the power is plugged in
        /// </summary>
        /// <param name="power"></param>
        /// <returns></returns>
        public static bool IsPluggedIn(this IPowerManager power) =>
            power.Status == PowerState.Charged ||
            power.Status == PowerState.Charging ||
            power.Status == PowerState.NoBattery;
    }
}
