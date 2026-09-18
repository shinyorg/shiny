namespace Shiny.ScreenRecorder;


/// <summary>
/// Rewords the notification Android shows for the life of a recording.
/// </summary>
/// <remarks>
/// <para>Register it next to <c>AddScreenRecorder()</c>. <see cref="IAndroidForegroundServiceDelegate.Configure"/>
/// receives the builder with the defaults already on it - a title, a line of text and a Stop action - so only
/// what should change needs setting. To relabel Stop, clear the actions and add your own with
/// <see cref="PlatformExtensions.GetScreenRecordingStopIntent"/>.</para>
/// <para>This module has no delegate of its own to hang <see cref="IAndroidForegroundServiceDelegate"/> on, the
/// way the GPS, beacon and transfer services do, so it gets a type of its own rather than reaching every
/// foreground service in the app.</para>
/// </remarks>
public interface IScreenRecordingNotificationDelegate : IAndroidForegroundServiceDelegate
{
}
