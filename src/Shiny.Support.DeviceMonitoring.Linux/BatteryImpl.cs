using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace Shiny.Power;


/// <summary>
/// Linux battery implementation backed by sysfs (/sys/class/power_supply).
/// </summary>
public class BatteryImpl : IBattery
{
    const string PowerSupplyRoot = "/sys/class/power_supply";


    public BatteryState Status
    {
        get
        {
            var dir = FindBatteryDir();
            if (dir == null)
                return BatteryState.None;

            var raw = ReadSysfs(Path.Combine(dir, "status"));
            return raw switch
            {
                "Charging" => BatteryState.Charging,
                "Discharging" => BatteryState.Discharging,
                "Full" => BatteryState.Full,
                "Not charging" => BatteryState.NotCharging,
                _ => BatteryState.Unknown
            };
        }
    }


    public double Level
    {
        get
        {
            var dir = FindBatteryDir();
            if (dir == null)
                return 1.0;

            var raw = ReadSysfs(Path.Combine(dir, "capacity"));
            if (int.TryParse(raw, out var pct))
                return Math.Clamp(pct / 100.0, 0.0, 1.0);

            return 1.0;
        }
    }


    public IObservable<IBattery> WhenChanged() => Observable
        .Interval(TimeSpan.FromSeconds(5))
        .Select(_ => (Status, Level))
        .DistinctUntilChanged()
        .Select(_ => (IBattery)this);


    static string? FindBatteryDir()
    {
        if (!Directory.Exists(PowerSupplyRoot))
            return null;

        // Pick first BAT* entry. Most laptops expose BAT0; some BAT1.
        return Directory
            .EnumerateDirectories(PowerSupplyRoot)
            .FirstOrDefault(d =>
            {
                var name = Path.GetFileName(d);
                return name.StartsWith("BAT", StringComparison.OrdinalIgnoreCase);
            });
    }


    static string? ReadSysfs(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
