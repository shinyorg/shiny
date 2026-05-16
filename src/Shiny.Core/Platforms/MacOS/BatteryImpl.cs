using System;
using System.Runtime.InteropServices;
using System.Threading;
using Foundation;
using ObjCRuntime;
using Shiny.Power;

namespace Shiny.Power;


public class BatteryImpl : IBattery
{
    const string IOKitLibrary = "/System/Library/Frameworks/IOKit.framework/IOKit";
    const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(IOKitLibrary)]
    static extern IntPtr IOPSCopyPowerSourcesInfo();

    [DllImport(IOKitLibrary)]
    static extern IntPtr IOPSCopyPowerSourcesList(IntPtr blob);

    [DllImport(IOKitLibrary)]
    static extern IntPtr IOPSGetPowerSourceDescription(IntPtr blob, IntPtr ps);

    [DllImport(IOKitLibrary)]
    static extern IntPtr IOPSNotificationCreateRunLoopSource(IOPSCallback callback, IntPtr context);

    [DllImport(CoreFoundationLibrary)]
    static extern void CFRelease(IntPtr cf);

    [DllImport(CoreFoundationLibrary)]
    static extern IntPtr CFRunLoopGetMain();

    [DllImport(CoreFoundationLibrary)]
    static extern void CFRunLoopAddSource(IntPtr runLoop, IntPtr source, IntPtr mode);

    [DllImport(CoreFoundationLibrary)]
    static extern void CFRunLoopRemoveSource(IntPtr runLoop, IntPtr source, IntPtr mode);

    static readonly IntPtr kCFRunLoopDefaultMode = LoadDefaultMode();

    static IntPtr LoadDefaultMode()
    {
        var lib = Dlfcn.dlopen(CoreFoundationLibrary, 0);
        if (lib == IntPtr.Zero)
            return IntPtr.Zero;
        try
        {
            return Dlfcn.GetIntPtr(lib, "kCFRunLoopDefaultMode");
        }
        finally
        {
            Dlfcn.dlclose(lib);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void IOPSCallback(IntPtr context);


    IOPSCallback? callback;
    IntPtr source;
    int subscriberCount;


    event EventHandler? changed;
    public event EventHandler? Changed
    {
        add
        {
            this.changed += value;
            if (Interlocked.Increment(ref this.subscriberCount) == 1)
                this.StartListening();
        }
        remove
        {
            this.changed -= value;
            if (Interlocked.Decrement(ref this.subscriberCount) == 0)
                this.StopListening();
        }
    }


    void StartListening()
    {
        this.callback = _ => this.changed?.Invoke(this, EventArgs.Empty);
        this.source = IOPSNotificationCreateRunLoopSource(this.callback, IntPtr.Zero);
        var runLoop = CFRunLoopGetMain();
        if (this.source != IntPtr.Zero && runLoop != IntPtr.Zero)
            CFRunLoopAddSource(runLoop, this.source, kCFRunLoopDefaultMode);
    }


    void StopListening()
    {
        if (this.source != IntPtr.Zero)
        {
            var runLoop = CFRunLoopGetMain();
            if (runLoop != IntPtr.Zero)
                CFRunLoopRemoveSource(runLoop, this.source, kCFRunLoopDefaultMode);
            CFRelease(this.source);
            this.source = IntPtr.Zero;
        }
        GC.KeepAlive(this.callback);
        this.callback = null;
    }


    public BatteryState Status => ReadPowerSource(
        fallback: BatteryState.None,
        reader: ps =>
        {
            var isCharging = ps.ObjectForKey((NSString)"Is Charging") is NSNumber charging && charging.BoolValue;
            var state = (ps.ObjectForKey((NSString)"Power Source State") as NSString)?.ToString();
            var level = ReadLevel(ps);

            if (state == "AC Power")
            {
                if (level >= 1.0)
                    return BatteryState.Full;
                return isCharging ? BatteryState.Charging : BatteryState.NotCharging;
            }
            return BatteryState.Discharging;
        }
    );


    public double Level => ReadPowerSource(fallback: 1.0, reader: ReadLevel);


    static double ReadLevel(NSDictionary ps)
    {
        var current = (ps.ObjectForKey((NSString)"Current Capacity") as NSNumber)?.DoubleValue ?? 0;
        var max = (ps.ObjectForKey((NSString)"Max Capacity") as NSNumber)?.DoubleValue ?? 100;
        if (max <= 0)
            return 0;
        return Math.Clamp(current / max, 0.0, 1.0);
    }


    static T ReadPowerSource<T>(T fallback, Func<NSDictionary, T> reader)
    {
        var blob = IOPSCopyPowerSourcesInfo();
        if (blob == IntPtr.Zero)
            return fallback;

        try
        {
            var listPtr = IOPSCopyPowerSourcesList(blob);
            if (listPtr == IntPtr.Zero)
                return fallback;

            try
            {
                var array = Runtime.GetNSObject<NSArray>(listPtr);
                if (array == null || array.Count == 0)
                    return fallback;

                var psPtr = IOPSGetPowerSourceDescription(blob, array.ValueAt(0));
                if (psPtr == IntPtr.Zero)
                    return fallback;

                var dict = Runtime.GetNSObject<NSDictionary>(psPtr);
                return dict == null ? fallback : reader(dict);
            }
            finally
            {
                CFRelease(listPtr);
            }
        }
        finally
        {
            CFRelease(blob);
        }
    }
}
