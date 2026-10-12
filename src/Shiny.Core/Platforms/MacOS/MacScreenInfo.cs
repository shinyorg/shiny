using System;
using AppKit;
using CoreFoundation;
using Foundation;

namespace Shiny;


/// <summary>
/// The macOS <see cref="IScreenInfo"/>, read from <see cref="NSScreen.MainScreen"/> - the screen holding the key
/// window. Mac windows do not rotate, so orientation cannot be locked; a rotated external display still reports
/// portrait.
/// </summary>
public class MacScreenInfo : AbstractScreenInfo
{
    protected override ScreenMetrics ReadMetrics() => OnMainThread(() =>
    {
        var screen = NSScreen.MainScreen;
        if (screen == null)
            return ScreenMetrics.Empty;

        var scale = (double)screen.BackingScaleFactor;
        var width = screen.Frame.Width * scale;
        var height = screen.Frame.Height * scale;
        var refresh = OperatingSystem.IsMacOSVersionAtLeast(12) ? screen.MaximumFramesPerSecond : 0;

        return new ScreenMetrics(width, height, scale, ScreenMetrics.OrientationOf(width, height), refresh);
    });


    protected override IDisposable Watch(Action refresh)
    {
        var center = NSNotificationCenter.DefaultCenter;
        var observers = new[]
        {
            // resolution, scale, arrangement and rotation changes
            center.AddObserver(NSApplication.DidChangeScreenParametersNotification, _ => refresh()),
            // the main screen follows the key window, so a window dragged to another display changes it
            center.AddObserver(NSWindow.DidChangeScreenNotification, _ => refresh())
        };

        return new ActionDisposable(() =>
        {
            foreach (var observer in observers)
                center.RemoveObserver(observer);
        });
    }


    static T OnMainThread<T>(Func<T> func)
    {
        if (NSThread.IsMain)
            return func();

        T result = default!;
        DispatchQueue.MainQueue.DispatchSync(() => result = func());
        return result;
    }
}
