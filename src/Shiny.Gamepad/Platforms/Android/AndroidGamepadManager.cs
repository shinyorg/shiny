using Android.App;
using Android.Content;
using Android.Hardware.Input;
using Android.Views;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>
/// Watches for controllers through <see cref="InputManager"/>, and routes their input out of the
/// focused activity.
/// </summary>
/// <remarks>
/// <para>Two halves that Android keeps apart. <see cref="InputManager"/> answers which controllers
/// exist and tells us when one arrives or leaves; it cannot read a single button. The input itself
/// only ever reaches the focused activity, so the activity's window callback is wrapped as it
/// resumes - see <see cref="GamepadWindowCallback"/> - and unwrapped as it goes away.</para>
/// <para>The hook follows the current activity through Shiny's activity lifecycle, so it survives
/// configuration changes and multi-activity navigation without the app doing anything.</para>
/// <para>Consequences of input being activity-scoped, which are Android's and not this library's:
/// a controller pressed while the app is backgrounded is not seen, and a controller that is
/// connected but never touched reports every stick centred until the player moves it.</para>
/// </remarks>
class AndroidGamepadManager : AbstractGamepadManager
{
    readonly AndroidPlatform platform;
    AndroidInputDeviceListener? listener;
    InputManager? inputManager;
    GamepadWindowCallback? callback;
    Activity? hooked;


    public AndroidGamepadManager(AndroidPlatform platform, ILogger<AndroidGamepadManager> logger) : base(logger)
        => this.platform = platform;


    /// <summary>
    /// Whether controller input is kept from the rest of the app. Off by default.
    /// </summary>
    /// <remarks>
    /// <para>Off, the events are observed and passed straight on, so the D-pad still moves focus,
    /// B still goes back and an on-screen keyboard still works. That is right for an app that uses
    /// a controller alongside its normal UI.</para>
    /// <para>On, controller key and motion events are reported here and then dropped, so nothing
    /// else in the app reacts to them. That is right for a game, where the D-pad moving focus
    /// around the view hierarchy is exactly the bug. Only events from a gamepad source are
    /// affected either way - touches, the keyboard and everything else are untouched.</para>
    /// </remarks>
    public bool ConsumeEvents { get; set; }


    protected override Task OnStart(CancellationToken ct)
    {
        this.inputManager = (InputManager?)this.platform.AppContext.GetSystemService(Context.InputService);
        if (this.inputManager == null)
            throw new GamepadException("Android did not provide an InputManager - gamepads cannot be enumerated");

        this.listener = new AndroidInputDeviceListener(this.TryAdd, this.OnDeviceRemoved, this.OnDeviceChanged);

        // a null handler means "call me on the main looper", which is where every other piece of
        // input in this backend already arrives - keeping them on one thread means the device list
        // and the window callback never race
        this.inputManager.RegisterInputDeviceListener(this.listener, null);

        var ids = InputDevice.GetDeviceIds() ?? [];
        foreach (var id in ids)
            this.TryAdd(id);

        this.platform.ActivityChanged += this.OnActivityChanged;
        this.platform.InvokeOnMainThread(() =>
        {
            if (this.platform.CurrentActivity is { } activity)
                this.Hook(activity);
        });

        this.Logger.WatchStarted(this.Current.Count);
        return Task.CompletedTask;
    }


    void OnActivityChanged(object? sender, ActivityChanged args)
    {
        switch (args.State)
        {
            case ActivityState.Resumed:
                this.Hook(args.Activity);
                break;

            case ActivityState.Paused:
                // every button is released as far as this app is concerned - the key-up for
                // anything still held will be delivered to whatever has focus now
                foreach (var gamepad in this.Current.OfType<AndroidGamepad>())
                    gamepad.Reset();
                break;

            case ActivityState.Destroyed:
                if (ReferenceEquals(args.Activity, this.hooked))
                    this.Unhook();
                break;
        }
    }


    void Hook(Activity activity)
    {
        if (ReferenceEquals(activity, this.hooked))
            return;

        this.Unhook();

        try
        {
            var window = activity.Window;
            if (window == null)
                return;

            // wrapping an existing wrapper would double every event and, worse, leave a chain that
            // Unhook cannot unwind - the activity is stored so a re-entrant call is a no-op
            if (window.Callback is GamepadWindowCallback)
                return;

            this.callback = new GamepadWindowCallback(window.Callback, this.OnKeyEvent, this.OnMotionEvent);
            window.Callback = this.callback;
            this.hooked = activity;

            this.Logger.WindowCallbackInstalled(activity.GetType().Name);
        }
        catch (Exception ex)
        {
            this.Logger.WindowCallbackFailed(ex);
        }
    }


    void Unhook()
    {
        if (this.hooked == null || this.callback == null)
            return;

        try
        {
            var window = this.hooked.Window;

            // only restore when our wrapper is still the one installed; something else may have
            // wrapped it since, and putting the old callback back would cut that out
            if (window != null && ReferenceEquals(window.Callback, this.callback))
                window.Callback = this.callback.Inner;
        }
        catch (Exception ex)
        {
            this.Logger.WindowCallbackFailed(ex);
        }

        this.callback = null;
        this.hooked = null;
    }


    bool OnKeyEvent(KeyEvent? e)
    {
        if (e == null || !AndroidKeyMap.IsGamepad(e.Source))
            return false;

        var gamepad = this.FindByDevice(e.DeviceId);
        if (gamepad == null)
        {
            // a controller can deliver its first event before InputManager announces it
            this.TryAdd(e.DeviceId);
            gamepad = this.FindByDevice(e.DeviceId);
        }

        var handled = gamepad?.HandleKey(e) ?? false;

        return handled && this.ConsumeEvents;
    }


    bool OnMotionEvent(MotionEvent? e)
    {
        if (e == null || !AndroidKeyMap.IsGamepad(e.Source))
            return false;

        var gamepad = this.FindByDevice(e.DeviceId);
        if (gamepad == null)
        {
            this.TryAdd(e.DeviceId);
            gamepad = this.FindByDevice(e.DeviceId);
        }

        var handled = gamepad?.HandleMotion(e) ?? false;

        return handled && this.ConsumeEvents;
    }


    AndroidGamepad? FindByDevice(int deviceId)
        => this.Current.OfType<AndroidGamepad>().FirstOrDefault(x => x.DeviceId == deviceId);


    void TryAdd(int deviceId)
    {
        var device = InputDevice.GetDevice(deviceId);
        if (device == null)
            return;

        if (!AndroidKeyMap.IsGamepad(device.Sources))
        {
            this.Logger.DeviceSkipped(deviceId, device.Sources.ToString());
            return;
        }

        // the descriptor is a hash of the device's identity rather than its connection, so it is
        // stable across reconnects - but Android explicitly warns it is not unique between two
        // identical controllers, so the runtime device id goes in too and PersistentId is not
        // claimed anywhere in this backend
        var id = $"android-{deviceId}-{device.Descriptor}";
        if (this.Find(id) != null)
            return;

        this.Add(new AndroidGamepad(id, device, this.Logger));
    }


    void OnDeviceRemoved(int deviceId)
    {
        var gamepad = this.FindByDevice(deviceId);
        if (gamepad != null)
            this.Remove(gamepad.Id);
    }


    void OnDeviceChanged(int deviceId)
    {
        // a device can gain or lose sources while connected - a dock that adds a second pad, a
        // controller switching mode - so a device that is now a gamepad and was not is picked up
        if (this.FindByDevice(deviceId) == null)
            this.TryAdd(deviceId);
    }
}
