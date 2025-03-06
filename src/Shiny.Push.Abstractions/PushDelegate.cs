using System.Threading.Tasks;

namespace Shiny.Push;


public interface IPushDelegate
{
    /// <summary>
    /// This is called when the user taps/responds to a push notification
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    Task OnEntry(PushNotification notification);


    /// <summary>
    /// Called when a push is received. BACKGROUND NOTE: if your app is in the background, you need to pass data parameters (iOS: content-available:1) to get this to fire
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    Task OnReceived(PushNotification notification);


    /// <summary>
    /// This is called when the token changes
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    Task OnNewToken(string token);

    /// <summary>
    /// This is fired when the user either denies permission (on a new app session) or IPushManager.UnRegister is called
    /// </summary>
    /// <returns></returns>
    Task OnUnRegistered(string token);
}


public class PushDelegate : IPushDelegate
{
    public virtual Task OnEntry(PushNotification notification) => Task.CompletedTask;
    public virtual Task OnReceived(PushNotification notification) => Task.CompletedTask;
    public virtual Task OnNewToken(string token) => Task.CompletedTask;
    public virtual Task OnUnRegistered(string token) => Task.CompletedTask;
}
