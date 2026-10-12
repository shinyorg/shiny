#if IOS
using UIKit;

namespace Shiny;


/// <summary>
/// The orientations <see cref="IScreenInfo.SetOrientation"/> currently allows on iOS - what holds the device there
/// after it has turned.
/// </summary>
/// <remarks>
/// <para>
/// UIKit asks the <b>app delegate</b> which orientations a window may take, and a library cannot answer for it.
/// Without this, <see cref="IScreenInfo.SetOrientation"/> still turns the device once, but nothing keeps it there -
/// tilting the phone turns it straight back. Return this from the app delegate to make it a lock:
/// </para>
/// <code>
/// [Export("application:supportedInterfaceOrientationsForWindow:")]
/// public UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow? forWindow)
///     => Shiny.IosOrientationLock.Supported;
/// </code>
/// <para>
/// <see cref="UIInterfaceOrientationMask.All"/> until something is locked, and again after
/// <see cref="IScreenInfo.ResetOrientation"/> - the app's Info.plist still has the final say over which of those
/// are allowed at all.
/// </para>
/// </remarks>
public static class IosOrientationLock
{
    public static UIInterfaceOrientationMask Supported { get; internal set; } = UIInterfaceOrientationMask.All;
}
#endif
