using System.Threading.Tasks;

namespace Shiny.Push;


/// <summary>
/// Consumer callback invoked by Shiny when push notifications are received, tapped, or when registration tokens change.
/// </summary>
public interface IPushDelegate
{
    /// <summary>
    /// Called when the user taps a push notification to launch or resume the app.
    /// </summary>
    /// <param name="notification">The notification payload the user tapped.</param>
    Task OnEntry(PushNotification notification);


    /// <summary>
    /// Called when a push notification is received. For background delivery the payload must include data fields (iOS requires content-available:1).
    /// </summary>
    /// <param name="notification">The received notification payload.</param>
    Task OnReceived(PushNotification notification);


    /// <summary>
    /// Called when the registration token changes (initial registration or token refresh).
    /// </summary>
    /// <param name="token">The new registration token.</param>
    Task OnNewToken(string token);

    /// <summary>
    /// Called when the device is unregistered, either because the user denied permission on a new session or <see cref="IPushManager.UnRegister"/> was invoked.
    /// </summary>
    /// <param name="token">The previously registered token.</param>
    Task OnUnRegistered(string token);
}


/// <summary>
/// Base <see cref="IPushDelegate"/> implementation that returns completed tasks; override only the callbacks you need.
/// </summary>
public class PushDelegate : IPushDelegate
{
    /// <summary>
    /// Called when the user taps a push notification to launch or resume the app.
    /// </summary>
    /// <param name="notification">The notification payload the user tapped.</param>
    public virtual Task OnEntry(PushNotification notification) => Task.CompletedTask;

    /// <summary>
    /// Called when a push notification is received in the foreground or background.
    /// </summary>
    /// <param name="notification">The received notification payload.</param>
    public virtual Task OnReceived(PushNotification notification) => Task.CompletedTask;

    /// <summary>
    /// Called when the registration token changes.
    /// </summary>
    /// <param name="token">The new registration token.</param>
    public virtual Task OnNewToken(string token) => Task.CompletedTask;

    /// <summary>
    /// Called when the device is unregistered from push notifications.
    /// </summary>
    /// <param name="token">The previously registered token.</param>
    public virtual Task OnUnRegistered(string token) => Task.CompletedTask;
}
