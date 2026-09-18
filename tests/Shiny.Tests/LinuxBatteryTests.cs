using System;
using System.IO;
using Shiny.Power;
using Xunit;

namespace Shiny.Tests;


/// <summary>
/// The Linux battery over a fake sysfs tree: /sys/class/power_supply and the ACPI platform profile.
/// </summary>
public class LinuxBatteryTests : IDisposable
{
    readonly string root = Path.Combine(Path.GetTempPath(), "shiny-battery-" + Guid.NewGuid().ToString("n"));
    readonly string supplies;
    readonly string profile;


    public LinuxBatteryTests()
    {
        this.supplies = Path.Combine(this.root, "power_supply");
        this.profile = Path.Combine(this.root, "platform_profile");
        Directory.CreateDirectory(this.supplies);
    }


    BatteryImpl Battery() => new(this.supplies, this.profile);


    void Supply(string name, params (string File, string Value)[] files)
    {
        var dir = Path.Combine(this.supplies, name);
        Directory.CreateDirectory(dir);
        foreach (var (file, value) in files)
            File.WriteAllText(Path.Combine(dir, file), value + "\n");
    }


    [Fact]
    public void A_laptop_on_its_battery()
    {
        this.Supply("BAT0", ("type", "Battery"), ("status", "Discharging"), ("capacity", "42"));
        this.Supply("AC", ("type", "Mains"), ("online", "0"));

        var battery = this.Battery();
        Assert.Equal(BatteryState.Discharging, battery.Status);
        Assert.Equal(0.42, battery.Level, 3);
        Assert.Equal(BatteryPowerSource.Battery, battery.PowerSource);
    }


    [Theory]
    [InlineData("Mains", BatteryPowerSource.AC)]
    [InlineData("USB", BatteryPowerSource.Usb)]
    [InlineData("Wireless", BatteryPowerSource.Wireless)]
    public void A_laptop_plugged_in(string type, BatteryPowerSource expected)
    {
        this.Supply("BAT0", ("type", "Battery"), ("status", "Charging"), ("capacity", "80"));
        this.Supply("CHARGER", ("type", type), ("online", "1"));

        var battery = this.Battery();
        Assert.Equal(BatteryState.Charging, battery.Status);
        Assert.Equal(expected, battery.PowerSource);
    }


    [Fact]
    public void A_machine_without_a_battery_is_on_mains()
    {
        var battery = this.Battery();
        Assert.Equal(BatteryState.None, battery.Status);
        Assert.Equal(1.0, battery.Level);
        Assert.Equal(BatteryPowerSource.AC, battery.PowerSource);
    }


    [Theory]
    [InlineData("low-power", EnergySaverStatus.On)]
    [InlineData("balanced", EnergySaverStatus.Off)]
    [InlineData("performance", EnergySaverStatus.Off)]
    public void Energy_saver_follows_the_platform_profile(string value, EnergySaverStatus expected)
    {
        File.WriteAllText(this.profile, value + "\n");
        Assert.Equal(expected, this.Battery().EnergySaverStatus);
    }


    [Fact]
    public void Energy_saver_is_unknown_without_a_platform_profile()
        => Assert.Equal(EnergySaverStatus.Unknown, this.Battery().EnergySaverStatus);


    public void Dispose()
    {
        try
        {
            Directory.Delete(this.root, true);
        }
        catch
        {
        }
    }
}
