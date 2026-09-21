using Microsoft.Extensions.Logging;

namespace Shiny.Gamepad.Infrastructure;


/// <summary>
/// The bookkeeping every backend's manager needs: starting the watch exactly once, keeping the
/// connected list, and raising connect and disconnect.
/// </summary>
/// <remarks>
/// <para>The watch starts lazily on the first <see cref="GetGamepads"/> rather than in the
/// constructor. Registering the service must not go near the hardware - a console app that
/// resolves <see cref="IGamepadManager"/> and never asks for a controller should not have opened a
/// device node, subscribed to a notification centre or hooked an activity's window.</para>
/// <para>Public for the two out-of-package backends, <c>Shiny.Gamepad.Linux</c> and
/// <c>Shiny.Gamepad.Blazor</c>. It is not an extension point for applications.</para>
/// </remarks>
public abstract class AbstractGamepadManager(ILogger logger) : IGamepadManager
{
    readonly Dictionary<string, AbstractGamepad> gamepads = new();
    readonly Lock listLock = new();
    readonly SemaphoreSlim startLock = new(1, 1);
    bool started;
    float axisChangeThreshold = 0.02f;
    TimeSpan pollInterval = TimeSpan.FromMilliseconds(16);


    /// <summary>The logger handed to the backend, for its own messages.</summary>
    protected ILogger Logger { get; } = logger;


    /// <inheritdoc/>
    public event EventHandler<GamepadConnectionEventArgs>? Connected;

    /// <inheritdoc/>
    public event EventHandler<GamepadConnectionEventArgs>? Disconnected;


    /// <inheritdoc/>
    public float AxisChangeThreshold
    {
        get => this.axisChangeThreshold;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1f);

            this.axisChangeThreshold = value;
            lock (this.listLock)
            {
                foreach (var gamepad in this.gamepads.Values)
                    gamepad.AxisChangeThreshold = value;
            }
        }
    }


    /// <inheritdoc/>
    public TimeSpan PollInterval
    {
        get => this.pollInterval;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, TimeSpan.Zero);
            this.pollInterval = value;
        }
    }


    /// <inheritdoc/>
    public async Task<IReadOnlyList<IGamepad>> GetGamepads(CancellationToken ct = default)
    {
        await this.EnsureStarted(ct).ConfigureAwait(false);

        lock (this.listLock)
            return this.gamepads.Values.ToList();
    }


    /// <summary>
    /// Starts the watch if it is not running. Every public entry point that needs live hardware
    /// goes through here first.
    /// </summary>
    protected async Task EnsureStarted(CancellationToken ct)
    {
        if (this.started)
            return;

        await this.startLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.started)
                return;

            await this.OnStart(ct).ConfigureAwait(false);
            this.started = true;
        }
        finally
        {
            this.startLock.Release();
        }
    }


    /// <summary>
    /// Opens whatever the platform needs, and calls <see cref="Add"/> for every controller already
    /// connected.
    /// </summary>
    /// <remarks>Runs once, under a lock. Throwing from here leaves the manager unstarted, so the next call retries.</remarks>
    protected abstract Task OnStart(CancellationToken ct);


    /// <summary>
    /// Registers a newly seen controller and raises <see cref="Connected"/>.
    /// </summary>
    /// <remarks>
    /// Ignores a controller whose <see cref="IGamepad.Id"/> is already known - every platform here
    /// will, in some sequence of sleep, resume and reconnect, announce the same pad twice.
    /// </remarks>
    protected void Add(AbstractGamepad gamepad)
    {
        lock (this.listLock)
        {
            if (!this.gamepads.TryAdd(gamepad.Id, gamepad))
                return;

            gamepad.AxisChangeThreshold = this.axisChangeThreshold;
        }

        this.Logger.GamepadConnected(gamepad.Id, gamepad.Name, gamepad.Kind);
        this.Raise(this.Connected, gamepad);
    }


    /// <summary>
    /// Marks a controller gone, drops it from the list and raises <see cref="Disconnected"/>.
    /// </summary>
    protected void Remove(string id)
    {
        AbstractGamepad? gamepad;
        lock (this.listLock)
        {
            if (!this.gamepads.Remove(id, out gamepad))
                return;
        }

        gamepad.SetDisconnected();
        this.Logger.GamepadDisconnected(id, gamepad.Name);
        this.Raise(this.Disconnected, gamepad);
    }


    /// <summary>The controller with this id, or null. Does not start the watch.</summary>
    protected AbstractGamepad? Find(string id)
    {
        lock (this.listLock)
            return this.gamepads.GetValueOrDefault(id);
    }


    /// <summary>Every controller currently tracked, as a snapshot safe to iterate. Does not start the watch.</summary>
    protected IReadOnlyList<AbstractGamepad> Current
    {
        get
        {
            lock (this.listLock)
                return this.gamepads.Values.ToList();
        }
    }


    void Raise(EventHandler<GamepadConnectionEventArgs>? handler, AbstractGamepad gamepad)
    {
        if (handler == null)
            return;

        var args = new GamepadConnectionEventArgs(gamepad);
        foreach (var invocation in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<GamepadConnectionEventArgs>)invocation).Invoke(this, args);
            }
            catch (Exception ex)
            {
                this.Logger.ConnectionHandlerThrew(gamepad.Id, ex);
            }
        }
    }
}
