using Foundation;
using GameController;
using Microsoft.Extensions.Logging;
using Shiny.Gamepad.Infrastructure;

namespace Shiny.Gamepad;


/// <summary>
/// Watches for controllers through GameController, on iOS, tvOS, Mac Catalyst and macOS.
/// </summary>
/// <remarks>
/// <para>One backend for all four platforms. GameController is the same framework everywhere Apple
/// ships it - the AppKit and UIKit split does not reach down to controllers - so macOS gets the
/// same code as iOS rather than a separate IOKit HID implementation.</para>
/// <para>Connects and disconnects arrive as notifications, so nothing polls. The notification
/// tokens are held for the life of the manager, which is the app's lifetime: the manager is a
/// singleton and there is no point at which it should stop hearing about controllers.</para>
/// <para>Apple hands out no durable identifier for a controller, so
/// <see cref="GamepadCapabilities.PersistentId"/> is never set here and <see cref="IGamepad.Id"/>
/// is good only for this connection.</para>
/// </remarks>
class AppleGamepadManager(ILogger<AppleGamepadManager> logger) : AbstractGamepadManager(logger)
{
    readonly Dictionary<nint, string> byHandle = new();
    NSObject? connectToken;
    NSObject? disconnectToken;
    int counter;


    protected override Task OnStart(CancellationToken ct)
    {
        this.connectToken = GCController.Notifications.ObserveDidConnect((_, args) => this.OnConnect(args));
        this.disconnectToken = GCController.Notifications.ObserveDidDisconnect((_, args) => this.OnDisconnect(args));

        var existing = GCController.Controllers;
        foreach (var controller in existing)
            this.Track(controller);

        this.Logger.WatchStarted(existing.Length);
        return Task.CompletedTask;
    }


    void OnConnect(NSNotificationEventArgs args)
    {
        if (args.Notification.Object is GCController controller)
            this.Track(controller);
    }


    void OnDisconnect(NSNotificationEventArgs args)
    {
        if (args.Notification.Object is not GCController controller)
            return;

        // the handle is the only thing that survives from connect to disconnect - GCController has
        // no identifier of its own, and the managed wrapper handed to the disconnect notification
        // is frequently a different instance to the one from the connect
        var handle = controller.Handle.Handle;
        lock (this.byHandle)
        {
            if (!this.byHandle.Remove(handle, out var id))
                return;

            this.Remove(id);
        }
    }


    void Track(GCController controller)
    {
        var handle = controller.Handle.Handle;
        string id;

        lock (this.byHandle)
        {
            if (this.byHandle.ContainsKey(handle))
                return;

            id = $"apple-{Interlocked.Increment(ref this.counter)}";
            this.byHandle[handle] = id;
        }

        this.Add(new AppleGamepad(id, controller, this.Logger));
    }
}
