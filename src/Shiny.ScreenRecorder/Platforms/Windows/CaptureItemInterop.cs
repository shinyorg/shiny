using System.Runtime.InteropServices;
using System.Text;
using Windows.Graphics.Capture;

namespace Shiny.ScreenRecorder;


/// <summary>
/// Builds a <see cref="GraphicsCaptureItem"/> for a monitor or window handle, and enumerates the
/// handles worth offering.
/// </summary>
/// <remarks>
/// <para><c>GraphicsCaptureItem</c> has no projected constructor. The only ways to make one are the
/// system picker (which needs a window handle and a user interaction) and
/// <c>IGraphicsCaptureItemInterop</c>, a COM interface on the class's own activation factory. This
/// uses the latter, so <see cref="IScreenRecorder.GetTargets"/> can return a real list the app
/// controls rather than forcing every recording through a picker dialog.</para>
/// <para>The factory is fetched with <c>RoGetActivationFactory</c> and called through its vtable
/// directly - no <c>ComImport</c>, for the same AOT reason as
/// <see cref="Direct3DInterop"/>.</para>
/// </remarks>
internal static unsafe partial class CaptureItemInterop
{
    static readonly Guid IID_IGraphicsCaptureItemInterop = new("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356");
    static readonly Guid IID_IGraphicsCaptureItem = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    const string GraphicsCaptureItemClassName = "Windows.Graphics.Capture.GraphicsCaptureItem";


    [LibraryImport("combase.dll")]
    private static partial int WindowsCreateString([MarshalAs(UnmanagedType.LPWStr)] string sourceString, int length, out IntPtr hstring);

    [LibraryImport("combase.dll")]
    private static partial int WindowsDeleteString(IntPtr hstring);

    [LibraryImport("combase.dll")]
    private static partial int RoGetActivationFactory(IntPtr activatableClassId, Guid* iid, out IntPtr factory);


    public static GraphicsCaptureItem CreateForMonitor(IntPtr monitor)
        => Create(monitor, vtableSlot: 4, "monitor");


    public static GraphicsCaptureItem CreateForWindow(IntPtr window)
        => Create(window, vtableSlot: 3, "window");


    // IGraphicsCaptureItemInterop: 3 IUnknown slots, then CreateForWindow (3), CreateForMonitor (4)
    static GraphicsCaptureItem Create(IntPtr handle, int vtableSlot, string kind)
    {
        var factory = GetInteropFactory();
        try
        {
            var iid = IID_IGraphicsCaptureItem;
            var vtable = *(void***)factory;
            var create = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, Guid*, out IntPtr, int>)vtable[vtableSlot];

            var hr = create(factory, handle, &iid, out var itemPtr);
            if (hr < 0 || itemPtr == IntPtr.Zero)
                throw new ScreenRecorderException($"Windows would not create a capture item for that {kind} (HRESULT 0x{hr:X8})");

            try
            {
                return GraphicsCaptureItem.FromAbi(itemPtr);
            }
            finally
            {
                Release(itemPtr);
            }
        }
        finally
        {
            Release(factory);
        }
    }


    static IntPtr GetInteropFactory()
    {
        var hr = WindowsCreateString(GraphicsCaptureItemClassName, GraphicsCaptureItemClassName.Length, out var className);
        if (hr < 0)
            throw new ScreenRecorderException($"Could not create the activation string (HRESULT 0x{hr:X8})");

        try
        {
            var iid = IID_IGraphicsCaptureItemInterop;
            hr = RoGetActivationFactory(className, &iid, out var factory);

            if (hr < 0 || factory == IntPtr.Zero)
                throw new ScreenRecorderException($"Windows.Graphics.Capture is not available on this system (HRESULT 0x{hr:X8})");

            return factory;
        }
        finally
        {
            WindowsDeleteString(className);
        }
    }


    static uint Release(IntPtr unknown)
    {
        var vtable = *(void***)unknown;
        var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)vtable[2];

        return release(unknown);
    }


    // ---- display and window enumeration -------------------------------------------------------

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => this.Right - this.Left;
        public readonly int Height => this.Bottom - this.Top;
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MonitorInfoEx
    {
        public int Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
    }


    const uint MonitorInfoPrimary = 1;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref Rect clip, IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate bool WindowEnumProc(IntPtr window, IntPtr data);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfoEx info);

    [DllImport("user32.dll")]
    static extern bool EnumWindows(WindowEnumProc callback, IntPtr data);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr window);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowTextLengthW(IntPtr window);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetWindowTextW(IntPtr window, [Out] char[] text, int count);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr window, out Rect rect);

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetShellWindow();


    internal record MonitorInfo(IntPtr Handle, string Name, int Width, int Height, bool IsPrimary);
    internal record WindowInfo(IntPtr Handle, string Title, int Width, int Height);


    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        // the callback is kept in a local so the delegate cannot be collected mid-enumeration,
        // which is the classic way this call crashes
        MonitorEnumProc callback = (monitor, _, ref _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };

            if (GetMonitorInfoW(monitor, ref info))
            {
                monitors.Add(new MonitorInfo(
                    monitor,
                    String.IsNullOrWhiteSpace(info.DeviceName) ? "Display" : info.DeviceName,
                    info.Monitor.Width,
                    info.Monitor.Height,
                    (info.Flags & MonitorInfoPrimary) != 0
                ));
            }

            return true;
        };

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        return monitors;
    }


    public static IReadOnlyList<WindowInfo> GetWindows()
    {
        var windows = new List<WindowInfo>();
        var shell = GetShellWindow();

        WindowEnumProc callback = (window, _) =>
        {
            // untitled and invisible windows are chrome, tooltips and message-only windows - none
            // of them are things a user would recognise in a picker, and the desktop shell window
            // captures as a black rectangle
            if (window == shell || !IsWindowVisible(window))
                return true;

            var length = GetWindowTextLengthW(window);
            if (length <= 0)
                return true;

            var buffer = new char[length + 1];
            var written = GetWindowTextW(window, buffer, buffer.Length);
            if (written <= 0)
                return true;

            if (!GetWindowRect(window, out var rect) || rect.Width <= 0 || rect.Height <= 0)
                return true;

            windows.Add(new WindowInfo(window, new string(buffer, 0, written), rect.Width, rect.Height));

            return true;
        };

        EnumWindows(callback, IntPtr.Zero);
        GC.KeepAlive(callback);

        return windows;
    }
}
