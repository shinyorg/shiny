#if APPLE
using System;
using System.Runtime.InteropServices;
using Foundation;

namespace Shiny;


static class AppleSystem
{
    static readonly NSString SystemTimeZoneDidChangeNotification = new("NSSystemTimeZoneDidChangeNotification");


    /// <summary>
    /// Reads a string value through sysctlbyname - "hw.machine" is the hardware identifier on iOS/tvOS
    /// ("iPhone16,1"), "hw.model" on a Mac ("Mac15,3"). No managed API exposes either.
    /// </summary>
    public static string? GetSysctl(string name)
    {
        nint length = 0;
        if (SysctlByName(name, IntPtr.Zero, ref length, IntPtr.Zero, 0) != 0 || length <= 0)
            return null;

        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            if (SysctlByName(name, buffer, ref length, IntPtr.Zero, 0) != 0)
                return null;

            return Marshal.PtrToStringAnsi(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }


    public static Version AppVersion
        => PlatformVersionParser.Parse(NSBundle.MainBundle.ObjectForInfoDictionary("CFBundleShortVersionString")?.ToString())
           ?? new Version(1, 0);


    public static IDisposable Observe(NSString notification, Action refresh)
    {
        var center = NSNotificationCenter.DefaultCenter;
        var token = center.AddObserver(notification, _ => refresh());
        return new ActionDisposable(() =>
        {
            center.RemoveObserver(token);
            token.Dispose();
        });
    }


    public static GlobalizationMonitor CreateGlobalizationMonitor(object sender) => new(
        sender,
        refresh => Observe(NSLocale.CurrentLocaleDidChangeNotification, refresh),
        refresh => Observe(SystemTimeZoneDidChangeNotification, refresh)
    );


    [DllImport("libc", EntryPoint = "sysctlbyname")]
    static extern int SysctlByName([MarshalAs(UnmanagedType.LPStr)] string name, IntPtr oldp, ref nint oldlenp, IntPtr newp, nint newlen);
}
#endif
