using System.Windows.Input;
using Shiny.Gamepad;

namespace Sample.Shared.Maui.Pages.Gamepads;

[ShellMap<GamepadPage>("gamepad")]
public partial class GamepadViewModel(IGamepadManager manager, IPlatform platform) : ObservableObject, IPageLifecycleAware
{
    static readonly GamepadLight[] Colours =
    [
        GamepadLight.FromRgb(255, 0, 0),
        GamepadLight.FromRgb(0, 255, 0),
        GamepadLight.FromRgb(0, 0, 255),
        GamepadLight.FromRgb(255, 0, 255)
    ];

    IReadOnlyList<IGamepad> connected = [];
    IGamepad? gamepad;
    int colourIndex;

    [ObservableProperty] List<GamepadItem> gamepads = [];
    [ObservableProperty] string status = "Looking for controllers...";
    [ObservableProperty] bool hasGamepad;
    [ObservableProperty] string name = "-";
    [ObservableProperty] string kind = "-";
    [ObservableProperty] string player = "-";
    [ObservableProperty] string capabilities = "-";
    [ObservableProperty] string battery = "-";
    [ObservableProperty] string lastButton = "-";
    [ObservableProperty] string buttons = "None";
    [ObservableProperty] string leftStick = "-";
    [ObservableProperty] string rightStick = "-";
    [ObservableProperty] string triggers = "-";
    [ObservableProperty] string motion = "-";
    [ObservableProperty] bool motionEnabled;
    [ObservableProperty] bool canVibrate;
    [ObservableProperty] bool canLight;
    [ObservableProperty] bool canMotion;
    [ObservableProperty] bool canBattery;

    public void OnAppearing()
    {
        manager.Connected += this.OnConnected;
        manager.Disconnected += this.OnDisconnected;
        _ = this.Load();
    }

    public void OnDisappearing()
    {
        manager.Connected -= this.OnConnected;
        manager.Disconnected -= this.OnDisconnected;
        this.Detach();
    }

    async Task Load()
    {
        try
        {
            // the first GetGamepads starts the watch - Connected/Disconnected only fire after it
            var list = await manager.GetGamepads();
            platform.InvokeOnMainThread(() => this.SetList(list));
        }
        catch (Exception ex)
        {
            platform.InvokeOnMainThread(() => this.Status = "Error: " + ex.Message);
        }
    }

    void OnConnected(object? sender, GamepadConnectionEventArgs e) => _ = this.Load();

    void OnDisconnected(object? sender, GamepadConnectionEventArgs e)
    {
        platform.InvokeOnMainThread(() =>
        {
            if (this.gamepad?.Id == e.Gamepad.Id)
                this.Detach();
        });
        _ = this.Load();
    }

    void SetList(IReadOnlyList<IGamepad> list)
    {
        this.connected = list;
        this.RebuildItems();

        this.Status = list.Count == 0
            ? "No controller connected - pair one over Bluetooth or plug it in"
            : $"{list.Count} controller(s) - tap one to select it";

        if (this.gamepad == null && list.Count > 0)
            this.Select(list[0]);
    }

    void Select(IGamepad g)
    {
        this.Detach();
        this.gamepad = g;
        g.ButtonChanged += this.OnButtonChanged;
        g.AxisChanged += this.OnAxisChanged;
        g.MotionChanged += this.OnMotionChanged;

        this.HasGamepad = true;
        this.Name = g.Name;
        this.Kind = g.Kind.ToString();
        this.Player = g.PlayerIndex?.ToString() ?? "Unassigned";
        this.Capabilities = g.Capabilities.ToString();
        this.CanVibrate = g.Supports(GamepadCapabilities.Vibration);
        this.CanLight = g.Supports(GamepadCapabilities.Light);
        this.CanMotion = g.Supports(GamepadCapabilities.Motion);
        this.CanBattery = g.Supports(GamepadCapabilities.Battery);
        this.LastButton = "-";
        this.Motion = "-";
        this.Render(g.GetState());

        this.RebuildItems();
        _ = this.RefreshBattery();
    }

    // rebuilt rather than mutated so the check mark follows the selection
    void RebuildItems() => this.Gamepads = this.connected
        .Select(g => new GamepadItem(
            $"{g.Name} ({g.Kind}){(g.Id == this.gamepad?.Id ? " ✓" : "")}",
            new Command(() => this.Select(g))
        ))
        .ToList();

    void Detach()
    {
        if (this.gamepad == null)
            return;

        this.gamepad.ButtonChanged -= this.OnButtonChanged;
        this.gamepad.AxisChanged -= this.OnAxisChanged;
        this.gamepad.MotionChanged -= this.OnMotionChanged;
        this.gamepad = null;
        this.HasGamepad = false;
        this.MotionEnabled = false;
    }

    void OnButtonChanged(object? sender, GamepadButtonChangedEventArgs e) => platform.InvokeOnMainThread(() =>
    {
        this.LastButton = $"{e.Button} {(e.IsPressed ? "pressed" : "released")}";
        this.Render(e.State);
    });

    void OnAxisChanged(object? sender, GamepadAxisChangedEventArgs e)
        => platform.InvokeOnMainThread(() => this.Render(e.State));

    void OnMotionChanged(object? sender, GamepadMotionChangedEventArgs e) => platform.InvokeOnMainThread(() =>
    {
        var m = e.Motion;
        this.Motion = $"Gyro {m.AngularVelocityX:F2}, {m.AngularVelocityY:F2}, {m.AngularVelocityZ:F2}\nAccel {m.AccelerationX:F2}, {m.AccelerationY:F2}, {m.AccelerationZ:F2}";
    });

    void Render(GamepadState state)
    {
        this.Buttons = state.Buttons == GamepadButton.None ? "None" : state.Buttons.ToString();
        this.LeftStick = $"{state.LeftStick.X:F2}, {state.LeftStick.Y:F2}";
        this.RightStick = $"{state.RightStick.X:F2}, {state.RightStick.Y:F2}";
        this.Triggers = $"L {state.LeftTrigger:P0}  R {state.RightTrigger:P0}";
    }

    async partial void OnMotionEnabledChanged(bool value)
    {
        if (this.gamepad == null || !this.CanMotion)
            return;

        try
        {
            await this.gamepad.SetMotionEnabled(value);
        }
        catch (Exception ex)
        {
            this.Motion = "Error: " + ex.Message;
        }
    }

    [RelayCommand]
    async Task Rumble()
    {
        if (this.gamepad == null)
            return;

        try
        {
            await this.gamepad.Pulse(0.8f, TimeSpan.FromMilliseconds(500));
        }
        catch (Exception ex)
        {
            this.Status = "Rumble failed: " + ex.Message;
        }
    }

    [RelayCommand]
    async Task CycleLight()
    {
        if (this.gamepad == null)
            return;

        try
        {
            this.colourIndex = (this.colourIndex + 1) % Colours.Length;
            await this.gamepad.SetLight(Colours[this.colourIndex]);
        }
        catch (Exception ex)
        {
            this.Status = "Light failed: " + ex.Message;
        }
    }

    [RelayCommand]
    async Task RefreshBattery()
    {
        if (this.gamepad == null || !this.CanBattery)
        {
            this.Battery = "Not reported";
            return;
        }

        try
        {
            var b = await this.gamepad.GetBattery();
            this.Battery = b.Level == null ? b.State.ToString() : $"{b.Level:P0} ({b.State})";
        }
        catch (Exception ex)
        {
            this.Battery = "Error: " + ex.Message;
        }
    }
}

public record GamepadItem(string Title, ICommand Select);
