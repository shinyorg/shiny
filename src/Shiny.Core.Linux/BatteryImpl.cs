using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Shiny.Power;
using Timer = System.Timers.Timer;


namespace Shiny.Power;


/// <summary>
/// Linux battery implementation backed by sysfs: /sys/class/power_supply for the battery and power source, and the
/// ACPI platform profile (which power-profiles-daemon sets to <c>low-power</c>) for energy saver.
/// Polls every 5 seconds when subscribers are attached.
/// </summary>
public class BatteryImpl : IBattery, IDisposable
{
    readonly string powerSupplyRoot;
    readonly string platformProfile;

    Timer? timer;
    int subscriberCount;
    BatteryState lastStatus;
    double lastLevel;
    BatteryPowerSource lastPowerSource;
    EnergySaverStatus lastEnergySaver;


    public BatteryImpl() : this("/sys/class/power_supply", "/sys/firmware/acpi/platform_profile") { }


    /// <summary>For tests: a sysfs tree somewhere else.</summary>
    internal BatteryImpl(string powerSupplyRoot, string platformProfile)
    {
        this.powerSupplyRoot = powerSupplyRoot;
        this.platformProfile = platformProfile;
    }


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
        this.lastPowerSource = this.PowerSource;
        this.lastEnergySaver = this.EnergySaverStatus;
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
        var powerSource = this.PowerSource;
        var energySaver = this.EnergySaverStatus;

        if (status != this.lastStatus
            || Math.Abs(level - this.lastLevel) > 0.001
            || powerSource != this.lastPowerSource
            || energySaver != this.lastEnergySaver)
        {
            this.lastStatus = status;
            this.lastLevel = level;
            this.lastPowerSource = powerSource;
            this.lastEnergySaver = energySaver;
            this.changed?.Invoke(this, EventArgs.Empty);
        }
    }


    public BatteryState Status
    {
        get
        {
            var dir = this.FindBatteryDir();
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
            var dir = this.FindBatteryDir();
            if (dir == null)
                return 1.0;

            var raw = ReadSysfs(Path.Combine(dir, "capacity"));
            if (int.TryParse(raw, out var pct))
                return Math.Clamp(pct / 100.0, 0.0, 1.0);

            return 1.0;
        }
    }


    /// <summary>
    /// The first external supply that is online (sysfs types <c>Mains</c>, <c>USB</c>, <c>Wireless</c>). With none, a
    /// machine with a battery is running on it, and one without is on mains power it does not report.
    /// </summary>
    public BatteryPowerSource PowerSource
    {
        get
        {
            if (Directory.Exists(this.powerSupplyRoot))
            {
                foreach (var dir in Directory.EnumerateDirectories(this.powerSupplyRoot))
                {
                    if (ReadSysfs(Path.Combine(dir, "online")) != "1")
                        continue;

                    var source = ReadSysfs(Path.Combine(dir, "type")) switch
                    {
                        "Mains" => BatteryPowerSource.AC,
                        "USB" => BatteryPowerSource.Usb,
                        "Wireless" => BatteryPowerSource.Wireless,
                        _ => BatteryPowerSource.Unknown
                    };

                    if (source != BatteryPowerSource.Unknown)
                        return source;
                }
            }

            return this.FindBatteryDir() == null ? BatteryPowerSource.AC : BatteryPowerSource.Battery;
        }
    }


    /// <summary>
    /// On when the ACPI platform profile is <c>low-power</c>, as power-profiles-daemon's power-saver profile sets it.
    /// Unknown on hardware without a platform profile.
    /// </summary>
    public EnergySaverStatus EnergySaverStatus => ReadSysfs(this.platformProfile) switch
    {
        null => EnergySaverStatus.Unknown,
        "low-power" => EnergySaverStatus.On,
        _ => EnergySaverStatus.Off
    };


    public void Dispose() => this.StopPolling();


    string? FindBatteryDir()
    {
        if (!Directory.Exists(this.powerSupplyRoot))
            return null;

        return Directory
            .EnumerateDirectories(this.powerSupplyRoot)
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
