namespace Shiny.Gamepad;


/// <summary>Conveniences built on top of <see cref="IGamepad"/> and <see cref="IGamepadManager"/>.</summary>
public static class GamepadExtensions
{
    /// <summary>
    /// Rumbles for a set time and then stops.
    /// </summary>
    /// <remarks>
    /// <para>The one-shot bump <see cref="IGamepad.SetVibration"/> deliberately is not: it sets the
    /// level, waits, and clears it. Awaiting it takes <paramref name="duration"/>, so fire it and
    /// forget it if the caller is a render loop.</para>
    /// <para>The motors are stopped even if the wait is cancelled or the controller disconnects
    /// mid-pulse, so a cancelled pulse never leaves a controller buzzing.</para>
    /// </remarks>
    /// <param name="gamepad">The controller.</param>
    /// <param name="vibration">Per-motor strength while the pulse runs.</param>
    /// <param name="duration">How long to run for.</param>
    /// <param name="ct">Cuts the pulse short. The motors still stop.</param>
    public static async Task Pulse(
        this IGamepad gamepad,
        GamepadVibration vibration,
        TimeSpan duration,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);

        await gamepad.SetVibration(vibration, ct).ConfigureAwait(false);
        try
        {
            await Task.Delay(duration, ct).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                await gamepad.SetVibration(GamepadVibration.Off, CancellationToken.None).ConfigureAwait(false);
            }
            catch (GamepadDisconnectedException)
            {
                // it unplugged mid-pulse, which stopped the motors more thoroughly than we could
            }
        }
    }


    /// <summary>Rumbles both handle motors at one strength for a set time.</summary>
    public static Task Pulse(this IGamepad gamepad, float intensity, TimeSpan duration, CancellationToken ct = default)
        => gamepad.Pulse(GamepadVibration.Both(intensity), duration, ct);


    /// <summary>
    /// Waits until any of <paramref name="buttons"/> is pressed, and reports which.
    /// </summary>
    /// <remarks>
    /// For "press any key to start" and for rebinding screens. Returns on the way down, not the way
    /// up, and stops waiting with a <see cref="GamepadDisconnectedException"/> if the controller
    /// goes away.
    /// </remarks>
    /// <param name="gamepad">The controller to watch.</param>
    /// <param name="buttons">Which buttons count. Defaults to any button at all.</param>
    /// <param name="ct">Abandons the wait.</param>
    public static async Task<GamepadButton> WaitForButton(
        this IGamepad gamepad,
        GamepadButton buttons = (GamepadButton)ulong.MaxValue,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(gamepad);

        var tcs = new TaskCompletionSource<GamepadButton>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnButton(object? sender, GamepadButtonChangedEventArgs args)
        {
            if (args.IsPressed && (args.Button & buttons) != GamepadButton.None)
                tcs.TrySetResult(args.Button);
        }

        gamepad.ButtonChanged += OnButton;
        try
        {
            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));

            // a controller that disconnects mid-wait would otherwise hang the caller forever - no
            // further ButtonChanged is ever coming from an inert gamepad
            using var timer = new Timer(
                _ =>
                {
                    if (!gamepad.IsConnected)
                        tcs.TrySetException(new GamepadDisconnectedException(gamepad.Id));
                },
                null,
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250)
            );

            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            gamepad.ButtonChanged -= OnButton;
        }
    }


    /// <summary>
    /// Waits for a controller to connect, returning immediately if one already has.
    /// </summary>
    /// <remarks>
    /// The usual opening move for a game: there is no controller when the app starts and the player
    /// turns one on a moment later. Starts the watch, so it does not need <see
    /// cref="IGamepadManager.GetGamepads"/> called first.
    /// </remarks>
    /// <param name="manager">The manager.</param>
    /// <param name="ct">Abandons the wait.</param>
    public static async Task<IGamepad> WaitForGamepad(this IGamepadManager manager, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var existing = await manager.GetGamepads(ct).ConfigureAwait(false);
        if (existing.Count > 0)
            return existing[0];

        var tcs = new TaskCompletionSource<IGamepad>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnConnected(object? sender, GamepadConnectionEventArgs args) => tcs.TrySetResult(args.Gamepad);

        manager.Connected += OnConnected;
        try
        {
            // one connecting between the enumeration above and the subscription would otherwise be
            // missed, and the caller would wait for a controller that is already on
            var raced = await manager.GetGamepads(ct).ConfigureAwait(false);
            if (raced.Count > 0)
                return raced[0];

            using var reg = ct.Register(() => tcs.TrySetCanceled(ct));
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            manager.Connected -= OnConnected;
        }
    }


    /// <summary>
    /// The controller the platform assigned to a player slot, or null.
    /// </summary>
    /// <param name="manager">The manager.</param>
    /// <param name="playerIndex">The slot, starting at 1.</param>
    /// <param name="ct">Cancels the lookup.</param>
    public static async Task<IGamepad?> GetGamepadForPlayer(
        this IGamepadManager manager,
        int playerIndex,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentOutOfRangeException.ThrowIfLessThan(playerIndex, 1);

        var gamepads = await manager.GetGamepads(ct).ConfigureAwait(false);
        return gamepads.FirstOrDefault(x => x.PlayerIndex == playerIndex);
    }


    /// <summary>Whether the controller can do everything in <paramref name="capabilities"/>.</summary>
    public static bool Supports(this IGamepad gamepad, GamepadCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(gamepad);
        return (gamepad.Capabilities & capabilities) == capabilities;
    }


    /// <summary>
    /// The left stick with a deadzone applied - the movement vector most games actually want.
    /// </summary>
    /// <param name="state">The snapshot to read.</param>
    /// <param name="deadzone">Radius to treat as centre, 0 to 1.</param>
    public static GamepadStick GetMovement(this GamepadState state, float deadzone = 0.15f)
        => state.LeftStick.WithDeadzone(deadzone);


    /// <summary>The right stick with a deadzone applied - the look vector.</summary>
    /// <param name="state">The snapshot to read.</param>
    /// <param name="deadzone">Radius to treat as centre, 0 to 1.</param>
    public static GamepadStick GetLook(this GamepadState state, float deadzone = 0.15f)
        => state.RightStick.WithDeadzone(deadzone);
}
