using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Evdev;
using Shiny.Gamepad.Infrastructure;
using Shiny.Gamepad.Sysfs;

namespace Shiny.Gamepad;


/// <summary>
/// Finds controllers by walking <c>/dev/input</c>, and watches it for ones that arrive later.
/// </summary>
/// <remarks>
/// <para>No udev, no D-Bus and no daemon. <c>/dev/input</c> is a real directory that the kernel
/// adds and removes nodes from, and inotify - which <see cref="FileSystemWatcher"/> uses on Linux -
/// reports those changes. That is the whole hotplug mechanism, and it works the same on a headless
/// Pi as on a full desktop, which a portal or a session-bus approach would not.</para>
/// <para>Most of <c>/dev/input</c> is not a controller: the power button, the lid switch, the
/// keyboard and the touchpad all live there too. A node qualifies only if it reports the kernel's
/// gamepad button (<c>BTN_SOUTH</c>) and at least one absolute axis, which is the same test
/// libmanette and SDL apply.</para>
/// <para>A controller with motion sensors appears as two nodes - the gamepad and an accelerometer
/// flagged with <c>INPUT_PROP_ACCELEROMETER</c> - which are paired by their shared HID parent in
/// sysfs so the gyro is offered on the controller it belongs to rather than as a second
/// controller.</para>
/// </remarks>
class LinuxGamepadManager(ILogger<LinuxGamepadManager> logger) : AbstractGamepadManager(logger), IDisposable
{
    const string InputDirectory = "/dev/input";

    readonly Lock scanLock = new();
    FileSystemWatcher? watcher;


    protected override Task OnStart(CancellationToken ct)
    {
        if (!OperatingSystem.IsLinux())
            throw new GamepadException("Shiny.Gamepad.Linux only runs on Linux - reference Shiny.Gamepad for the other platforms");

        if (!Directory.Exists(InputDirectory))
            throw new GamepadException($"'{InputDirectory}' does not exist - this kernel has no evdev interface");

        this.Scan();

        this.watcher = new FileSystemWatcher(InputDirectory, "event*")
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true
        };
        this.watcher.Created += this.OnNodeCreated;
        this.watcher.Deleted += (_, _) => this.Scan();

        this.Logger.WatchStarted(this.Current.Count);
        return Task.CompletedTask;
    }


    void OnNodeCreated(object? sender, FileSystemEventArgs args)
    {
        // the node exists the moment inotify fires, but its permissions are set by a udev rule that
        // has not necessarily run yet - opening immediately gets EACCES on a controller that will
        // be perfectly readable a moment later
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250)).ConfigureAwait(false);
            this.Scan();
        });
    }


    void Scan()
    {
        lock (this.scanLock)
        {
            try
            {
                var nodes = Directory
                    .EnumerateFiles(InputDirectory, "event*")
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                var live = new HashSet<string>();

                foreach (var node in nodes)
                {
                    var existing = this.Current.OfType<LinuxGamepad>().FirstOrDefault(x => x.Path == node);
                    if (existing != null)
                    {
                        live.Add(existing.Id);
                        continue;
                    }

                    var added = this.TryAdd(node, nodes);
                    if (added != null)
                        live.Add(added);
                }

                foreach (var known in this.Current)
                {
                    if (!live.Contains(known.Id))
                        this.Remove(known.Id);
                }
            }
            catch (Exception ex)
            {
                this.Logger.PollLoopFailed(ex);
            }
        }
    }


    string? TryAdd(string node, IReadOnlyList<string> allNodes)
    {
        EvdevDevice? device = null;
        EvdevDevice? motion = null;

        try
        {
            device = EvdevDevice.TryOpen(node);
            if (device == null)
                return null;

            if (!IsGamepad(device))
            {
                device.Dispose();
                return null;
            }

            motion = FindMotionNode(node, allNodes);
            var sysfs = SysfsDevice.Discover(node);

            // the uniq is the controller's own serial or MAC, which is what makes an id survive a
            // reconnect; a controller that reports none falls back to its node path, which does not
            var id = device.Unique is { Length: > 0 } unique
                ? $"linux-{unique}"
                : $"linux-node-{Path.GetFileName(node)}";

            if (this.Find(id) != null)
            {
                device.Dispose();
                motion?.Dispose();
                return id;
            }

            var gamepad = new LinuxGamepad(id, device, motion, sysfs, this.Logger);
            this.Add(gamepad);
            gamepad.Start();

            return id;
        }
        catch (Exception ex)
        {
            device?.Dispose();
            motion?.Dispose();
            this.Logger.ReadFailed(node, ex);

            return null;
        }
    }


    static bool IsGamepad(EvdevDevice device)
        => device.Buttons.Contains(EvdevCodes.BTN_SOUTH) &&
           device.Axes.Contains(EvdevCodes.ABS_X) &&
           !device.IsAccelerometer;


    static EvdevDevice? FindMotionNode(string gamepadNode, IReadOnlyList<string> allNodes)
    {
        var parent = ResolveHidParent(gamepadNode);
        if (parent == null)
            return null;

        foreach (var candidate in allNodes)
        {
            if (candidate == gamepadNode)
                continue;

            if (!string.Equals(ResolveHidParent(candidate), parent, StringComparison.Ordinal))
                continue;

            var device = EvdevDevice.TryOpen(candidate);
            if (device == null)
                continue;

            if (device.IsAccelerometer)
                return device;

            device.Dispose();
        }

        return null;
    }


    static string? ResolveHidParent(string node)
    {
        try
        {
            var device = $"/sys/class/input/{Path.GetFileName(node)}/device/device";

            return Directory.Exists(device) ? Path.GetFullPath(device) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }


    public void Dispose()
    {
        this.watcher?.Dispose();
        this.watcher = null;

        foreach (var gamepad in this.Current)
            gamepad.SetDisconnected();
    }
}
