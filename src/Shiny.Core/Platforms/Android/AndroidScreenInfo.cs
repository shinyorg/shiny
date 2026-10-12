using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.PM;
using Android.Hardware.Display;
using Android.OS;
using Android.Util;
using Android.Views;

namespace Shiny;


/// <summary>
/// The Android <see cref="IScreenInfo"/> - the default display's real size (including system bars), watched
/// through <see cref="DisplayManager"/>.
/// </summary>
public class AndroidScreenInfo(AndroidPlatform platform) : AbstractScreenInfo
{
    DisplayManager Displays => (DisplayManager)platform.AppContext.GetSystemService(Context.DisplayService)!;


    protected override ScreenMetrics ReadMetrics()
    {
        var display = this.Displays.GetDisplay(Display.DefaultDisplay);
        if (display == null)
            return ScreenMetrics.Empty;

        var metrics = new DisplayMetrics();
#pragma warning disable CA1422 // the replacement, WindowMetrics, needs a window context - this has to work from a service
        display.GetRealMetrics(metrics);
#pragma warning restore CA1422

        return new ScreenMetrics(
            metrics.WidthPixels,
            metrics.HeightPixels,
            metrics.Density,
            ScreenMetrics.OrientationOf(metrics.WidthPixels, metrics.HeightPixels),
            display.RefreshRate
        );
    }


    protected override IDisposable Watch(Action refresh)
    {
        var listener = new DisplayListener(refresh);
        var manager = this.Displays;
        manager.RegisterDisplayListener(listener, new Handler(Looper.MainLooper!));

        return new ActionDisposable(() =>
        {
            manager.UnregisterDisplayListener(listener);
            listener.Dispose();
        });
    }


    // The sensor variants still let the device flip between left and right (or upside down) within the
    // chosen orientation, rather than locking to one physical rotation.
    protected override Task<bool> ApplyOrientation(DeviceOrientation orientation)
    {
        var activity = platform.CurrentActivity;
        if (activity == null)
            return Task.FromResult(false);

        var requested = orientation switch
        {
            DeviceOrientation.Portrait => ScreenOrientation.SensorPortrait,
            DeviceOrientation.Landscape => ScreenOrientation.SensorLandscape,
            _ => ScreenOrientation.Unspecified
        };
        platform.InvokeOnMainThread(() => activity.RequestedOrientation = requested);
        return Task.FromResult(true);
    }


    sealed class DisplayListener(Action refresh) : Java.Lang.Object, DisplayManager.IDisplayListener
    {
        public void OnDisplayAdded(int displayId) { }
        public void OnDisplayRemoved(int displayId) { }

        public void OnDisplayChanged(int displayId)
        {
            if (displayId == Display.DefaultDisplay)
                refresh();
        }
    }
}
