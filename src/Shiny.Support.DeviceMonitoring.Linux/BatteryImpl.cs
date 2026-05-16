using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Shiny.Power;
using Timer = System.Timers.Timer;


namespace Shiny.Power;


/// <summary>
/// Linux battery implementation backed by sysfs (/sys/class/power_supply).
/// Polls every 5 seconds when subscribers are attached.
/// </summary>
public class BatteryImpl : IBattery, IDisposable
{
    const string PowerSupplyRoot = "/sys/class/power_supply";

    Timer? timer;
    int subscriberCount;
    BatteryState lastStatus;
    double lastLevel;


    event EventHandler? changed;
    public event EventHandler? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
                this.StartPolling();
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
                this.StopPolling();
        }
    }


    void StartPolling()
    {
        this.lastStatus = this.Status;
        this.lastLevel = this.Level;
        this.timer = new Timer(TimeSpan.FromSeconds(5));
        this.timer.Elapsed += this.OnTick;
        this.timer.Start();
    }


    void StopPolling()
    {
        this.timer?.Stop();
        this.timer?.Dispose();
        this.timer = null;
    }


    void OnTick(object? sender, ElapsedEventArgs e)
    {
        var status = this.Status;
        var level = this.Level;
        if (status != this.lastStatus || Math.Abs(level - this.lastLevel) > 0.001)
        {
            this.lastStatus = status;
            this.lastLevel = level;
            this.changed?.Invoke(this, EventArgs.Empty);
        }
    }


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


    public void Dispose() => this.StopPolling();


    static string? FindBatteryDir()
    {
        if (!Directory.Exists(PowerSupplyRoot))
            return null;

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
