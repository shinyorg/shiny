namespace Shiny.Gamepad.Sysfs;


/// <summary>
/// The battery and LEDs a controller exposes through sysfs.
/// </summary>
/// <remarks>
/// <para>evdev carries input and force feedback and nothing else. A controller's battery and its
/// light bar are published by its HID driver as sysfs attributes instead, hanging off the same
/// parent device the event node does - so finding them means walking from
/// <c>/sys/class/input/eventN</c> up to the HID device and looking for
/// <c>power_supply</c> and <c>leds</c> underneath it.</para>
/// <para>Every path here is read at construction and cached. They do not move while the controller
/// is connected, and a controller that disconnects takes its whole sysfs subtree with it, so
/// nothing has to be revalidated - the reads simply start failing, which is reported as no
/// reading.</para>
/// </remarks>
class SysfsDevice
{
    SysfsDevice(string? batteryDirectory, string? lightDirectory)
    {
        this.BatteryDirectory = batteryDirectory;
        this.LightDirectory = lightDirectory;
    }


    /// <summary>The <c>power_supply</c> directory for this controller, or null.</summary>
    public string? BatteryDirectory { get; }

    /// <summary>The RGB LED directory for this controller, or null.</summary>
    public string? LightDirectory { get; }

    public bool HasBattery => this.BatteryDirectory != null;
    public bool HasLight => this.LightDirectory != null;


    /// <summary>Finds what sysfs publishes for the controller behind an event node.</summary>
    /// <param name="eventNode">The device node, such as <c>/dev/input/event5</c>.</param>
    public static SysfsDevice Discover(string eventNode)
    {
        var parent = FindHidDirectory(eventNode);
        if (parent == null)
            return new SysfsDevice(null, null);

        return new SysfsDevice(FindBattery(parent), FindLight(parent));
    }


    static string? FindHidDirectory(string eventNode)
    {
        try
        {
            var name = Path.GetFileName(eventNode);

            // /sys/class/input/eventN/device is the input device; its own device attribute is the
            // HID (or USB) device that owns it, and that is where power_supply and leds live
            var device = $"/sys/class/input/{name}/device/device";

            return Directory.Exists(device) ? Path.GetFullPath(device) : null;
        }
        catch (Exception)
        {
            // a controller unplugged mid-enumeration leaves paths that vanish between the check and
            // the read; no battery and no light is the right answer, not a failure
            return null;
        }
    }


    static string? FindBattery(string hidDirectory)
    {
        try
        {
            var supplies = Path.Combine(hidDirectory, "power_supply");
            if (!Directory.Exists(supplies))
                return null;

            return Directory
                .EnumerateDirectories(supplies)
                .FirstOrDefault(x => File.Exists(Path.Combine(x, "capacity")));
        }
        catch (Exception)
        {
            return null;
        }
    }


    static string? FindLight(string hidDirectory)
    {
        try
        {
            var leds = Path.Combine(hidDirectory, "leds");
            if (!Directory.Exists(leds))
                return null;

            // hid-playstation publishes a DualSense's light bar as a multi-colour LED named
            // "<input>:rgb:indicator", written through multi_intensity as three space-separated
            // channels. The player-number LEDs alongside it are single-intensity and are left
            // alone - they are the platform's to drive, not the app's.
            return Directory
                .EnumerateDirectories(leds)
                .FirstOrDefault(x => File.Exists(Path.Combine(x, "multi_intensity")));
        }
        catch (Exception)
        {
            return null;
        }
    }


    /// <summary>Reads the battery, or null when it cannot be read.</summary>
    public GamepadBattery? ReadBattery()
    {
        if (this.BatteryDirectory == null)
            return null;

        try
        {
            var capacityPath = Path.Combine(this.BatteryDirectory, "capacity");
            var statusPath = Path.Combine(this.BatteryDirectory, "status");

            float? level = File.Exists(capacityPath) && int.TryParse(File.ReadAllText(capacityPath).Trim(), out var capacity)
                ? Math.Clamp(capacity / 100f, 0f, 1f)
                : null;

            var status = File.Exists(statusPath) ? File.ReadAllText(statusPath).Trim() : string.Empty;
            var state = status switch
            {
                "Charging" => GamepadBatteryState.Charging,
                "Discharging" => GamepadBatteryState.Discharging,
                "Full" => GamepadBatteryState.Full,
                "Not charging" => GamepadBatteryState.Wired,
                _ => GamepadBatteryState.Unknown
            };

            return new GamepadBattery(level, state);
        }
        catch (Exception)
        {
            return null;
        }
    }


    /// <summary>Sets the light bar colour. Returns false when sysfs refused the write.</summary>
    /// <remarks>
    /// Writing to <c>/sys/class/leds</c> needs permission the desktop session usually does not
    /// have. A udev rule granting the seat's user write access to the controller's LED is the
    /// normal fix, and until one is in place this returns false rather than throwing - a light that
    /// will not change is not a reason to fail the frame.
    /// </remarks>
    public bool WriteLight(GamepadLight light)
    {
        if (this.LightDirectory == null)
            return false;

        try
        {
            var path = Path.Combine(this.LightDirectory, "multi_intensity");
            var red = (int)MathF.Round(Math.Clamp(light.Red, 0f, 1f) * 255f);
            var green = (int)MathF.Round(Math.Clamp(light.Green, 0f, 1f) * 255f);
            var blue = (int)MathF.Round(Math.Clamp(light.Blue, 0f, 1f) * 255f);

            File.WriteAllText(path, $"{red} {green} {blue}");

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
