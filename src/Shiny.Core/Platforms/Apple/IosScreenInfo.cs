using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using UIKit;

namespace Shiny;


/// <summary>
/// The iOS / Mac Catalyst / tvOS <see cref="IScreenInfo"/>, read from <see cref="UIScreen.MainScreen"/>. Its bounds
/// follow the interface orientation, so width against height is the orientation the app is shown in.
/// </summary>
public class IosScreenInfo : AbstractScreenInfo
{
    /// <summary>How long a rotation is given to finish before the display is read again.</summary>
    static readonly TimeSpan RotationSettle = TimeSpan.FromMilliseconds(700);


    protected override ScreenMetrics ReadMetrics() => OnMainThread(() =>
    {
        var screen = UIScreen.MainScreen;
        var bounds = screen.Bounds;
        var scale = (double)screen.Scale;
        var width = bounds.Width * scale;
        var height = bounds.Height * scale;

        return new ScreenMetrics(
            width,
            height,
            scale,
#if TVOS
            DeviceOrientation.Landscape,
#else
            ScreenMetrics.OrientationOf(width, height),
#endif
            screen.MaximumFramesPerSecond
        );
    });


    protected override IDisposable Watch(Action refresh)
    {
        var center = NSNotificationCenter.DefaultCenter;
        var observers = new System.Collections.Generic.List<NSObject>
        {
            // a resolution or scale change, and an external display taking over
            center.AddObserver(UIScreen.ModeDidChangeNotification, _ => refresh())
        };

#if !TVOS
        // The device notification arrives before the interface has turned, so the display is read again once
        // the rotation has had time to finish - a reading that has not changed raises nothing.
        DispatchQueue.MainQueue.DispatchAsync(() => UIDevice.CurrentDevice.BeginGeneratingDeviceOrientationNotifications());
        observers.Add(center.AddObserver(UIDevice.OrientationDidChangeNotification, notification =>
        {
            refresh();
            _ = Task.Delay(RotationSettle).ContinueWith(_ => refresh());
        }));
#endif

        return new ActionDisposable(() =>
        {
            foreach (var observer in observers)
                center.RemoveObserver(observer);

#if !TVOS
            DispatchQueue.MainQueue.DispatchAsync(() => UIDevice.CurrentDevice.EndGeneratingDeviceOrientationNotifications());
#endif
        });
    }


#if IOS
    // iOS 16+ rotates through UIWindowScene.RequestGeometryUpdate; before that there is no public API. Mac
    // Catalyst and tvOS windows do not rotate.
    //
    // Three things this has to get right:
    //
    // - RequestGeometryUpdate's handler is called on FAILURE ONLY, so it cannot complete the task. The result
    //   is whether the display ended up in the requested orientation, read once the rotation has had time - the
    //   request's own verdict is not reliable either, because it is refused against a view controller's stale
    //   supported orientations even while the narrowed mask is turning the device.
    // - The scene is the app's own foreground one. ConnectedScenes also holds an external display's scene
    //   (AirPlay), and rotating that is not what anybody asked for.
    // - A request is a single rotation, not a lock. What holds the device is the app's allowed mask, which
    //   UIKit asks the app delegate for - so it is kept in IosOrientationLock.Supported for the delegate to
    //   return, and every view controller is told to re-read it.
    protected override Task<bool> ApplyOrientation(DeviceOrientation orientation)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16))
            return Task.FromResult(false);

        return this.RequestGeometry(orientation);
    }


    [SupportedOSPlatform("ios16.0")]
    Task<bool> RequestGeometry(DeviceOrientation orientation)
    {
        var mask = orientation switch
        {
            DeviceOrientation.Portrait => UIInterfaceOrientationMask.Portrait,
            DeviceOrientation.Landscape => UIInterfaceOrientationMask.Landscape,
            _ => UIInterfaceOrientationMask.All
        };

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            IosOrientationLock.Supported = mask;

            var scene = UIApplication.SharedApplication.ConnectedScenes
                .ToArray()
                .OfType<UIWindowScene>()
                .Where(x => x.Session.Role == UIWindowSceneSessionRole.Application)
                .OrderByDescending(x => x.ActivationState == UISceneActivationState.ForegroundActive)
                .FirstOrDefault();

            if (scene == null)
            {
                tcs.TrySetResult(false);
                return;
            }

            foreach (var window in scene.Windows)
                RefreshSupportedOrientations(window.RootViewController);

            // clearing the lock turns nothing - the device stays where it is until it is rotated
            if (mask == UIInterfaceOrientationMask.All)
            {
                tcs.TrySetResult(true);
                return;
            }

            // On the next turn of the main loop: view controllers re-read their supported orientations
            // asynchronously, and a request made in the same turn is refused against the old ones.
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                scene.RequestGeometryUpdate(new UIWindowSceneGeometryPreferencesIOS(mask), _ => { });
                _ = Task.Delay(RotationSettle).ContinueWith(_ =>
                    tcs.TrySetResult(this.ReadMetrics().Orientation == orientation)
                );
            });
        });
        return tcs.Task;
    }


    // Every controller, not only the root: under Shell (tab bar -> navigation -> page) the one UIKit asks is a
    // child, and one left with its old answer refuses the change.
    [SupportedOSPlatform("ios16.0")]
    static void RefreshSupportedOrientations(UIViewController? controller)
    {
        if (controller == null)
            return;

        controller.SetNeedsUpdateOfSupportedInterfaceOrientations();
        foreach (var child in controller.ChildViewControllers)
            RefreshSupportedOrientations(child);

        RefreshSupportedOrientations(controller.PresentedViewController);
    }
#endif


    static T OnMainThread<T>(Func<T> func)
    {
        if (NSThread.IsMain)
            return func();

        T result = default!;
        DispatchQueue.MainQueue.DispatchSync(() => result = func());
        return result;
    }
}
