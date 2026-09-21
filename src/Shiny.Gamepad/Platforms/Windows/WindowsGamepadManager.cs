using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;
using Windows.Gaming.Input;
using NativeGamepad = Windows.Gaming.Input.Gamepad;

namespace Shiny.Gamepad;


/// <summary>
/// Watches for controllers through <c>Windows.Gaming.Input</c>, and polls them.
/// </summary>
/// <remarks>
/// <para>Windows has no way to be told about input, so a timer reads every connected controller at
/// <see cref="IGamepadManager.PollInterval"/> and the readings become state changes and events.
/// The same loop reconciles the connected list.</para>
/// <para>It reconciles rather than relying only on <c>GamepadAdded</c> and <c>GamepadRemoved</c>
/// because those static events are delivered through the app's message pump: a console host, a
/// background thread or a window that has not pumped recently will miss them entirely. Subscribing
/// as well means a desktop app hears about a controller the moment it arrives, and reconciling
/// means a host with no pump still finds it on the next tick.</para>
/// </remarks>
class WindowsGamepadManager(ILogger<WindowsGamepadManager> logger) : AbstractGamepadManager(logger)
{
    readonly Lock pollLock = new();
    Timer? timer;
    int reentrancyGuard;


    protected override Task OnStart(CancellationToken ct)
    {
        NativeGamepad.GamepadAdded += this.OnGamepadAdded;
        NativeGamepad.GamepadRemoved += this.OnGamepadRemoved;

        this.Reconcile();

        this.timer = new Timer(_ => this.Tick(), null, this.PollInterval, this.PollInterval);

        this.Logger.WatchStarted(this.Current.Count);
        return Task.CompletedTask;
    }


    void OnGamepadAdded(object? sender, NativeGamepad e) => this.Reconcile();
    void OnGamepadRemoved(object? sender, NativeGamepad e) => this.Reconcile();


    void Tick()
    {
        // a poll that overruns its interval would otherwise stack timer callbacks on top of each
        // other; dropping the tick is right, because the next one reads the same hardware anyway
        if (Interlocked.Exchange(ref this.reentrancyGuard, 1) == 1)
            return;

        try
        {
            this.Reconcile();

            foreach (var gamepad in this.Current.OfType<WindowsGamepad>())
            {
                try
                {
                    gamepad.Poll();
                }
                catch (Exception ex)
                {
                    // a controller yanked between the reconcile and the read throws from the WinRT
                    // proxy; treating that as a disconnect is exactly what happened
                    this.Logger.ReadFailed(gamepad.Id, ex);
                    this.Remove(gamepad.Id);
                }
            }

            // PollInterval can be changed at any time, and a timer created with the old one would
            // keep the old cadence forever
            this.timer?.Change(this.PollInterval, this.PollInterval);
        }
        catch (Exception ex)
        {
            this.Logger.PollLoopFailed(ex);
        }
        finally
        {
            Interlocked.Exchange(ref this.reentrancyGuard, 0);
        }
    }


    void Reconcile()
    {
        lock (this.pollLock)
        {
            var native = NativeGamepad.Gamepads;
            var seen = new HashSet<string>(native.Count);

            for (var index = 0; index < native.Count; index++)
            {
                var pad = native[index];
                var raw = RawGameController.FromGameController(pad);

                // NonRoamableId names a specific physical controller on this machine and survives
                // reconnects and reboots, which is what lets this backend claim PersistentId. A
                // controller that will not produce one falls back to its index, which does not -
                // but an unstable id is still better than refusing to report the controller.
                var id = raw?.NonRoamableId is { Length: > 0 } nonRoamable
                    ? $"windows-{nonRoamable}"
                    : $"windows-index-{index}";

                seen.Add(id);

                if (this.Find(id) == null)
                    this.Add(new WindowsGamepad(id, pad, raw, this.Logger));
            }

            foreach (var known in this.Current)
            {
                if (!seen.Contains(known.Id))
                    this.Remove(known.Id);
            }
        }
    }
}
