using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Shiny;


/// <summary>
/// The Windows <see cref="IScreenInfo"/> - the primary monitor's current mode, read through Win32 so it works the same
/// for WinUI, WPF, WinForms and console apps.
/// </summary>
public class WindowsScreenInfo : AbstractScreenInfo
{
    const int EnumCurrentSettings = -1;
    const int OrientationPreferenceNone = 0x0;
    const int OrientationPreferenceLandscape = 0x1;
    const int OrientationPreferencePortrait = 0x2;
    const int OrientationPreferenceLandscapeFlipped = 0x4;
    const int OrientationPreferencePortraitFlipped = 0x8;


    protected override ScreenMetrics ReadMetrics()
    {
        var mode = new DevMode { dmSize = (short)Marshal.SizeOf<DevMode>() };
        if (!EnumDisplaySettings(null, EnumCurrentSettings, ref mode))
            return ScreenMetrics.Empty;

        var width = (double)mode.dmPelsWidth;
        var height = (double)mode.dmPelsHeight;
        var dpi = GetDpiForSystem();

        return new ScreenMetrics(
            width,
            height,
            dpi > 0 ? dpi / 96.0 : 1.0,
            ScreenMetrics.OrientationOf(width, height),
            mode.dmDisplayFrequency > 1 ? mode.dmDisplayFrequency : 0 // 0 and 1 both mean "hardware default"
        );
    }


    protected override IDisposable Watch(Action refresh)
    {
        EventHandler handler = (_, _) => refresh();
        SystemEvents.DisplaySettingsChanged += handler;
        return new ActionDisposable(() => SystemEvents.DisplaySettingsChanged -= handler);
    }


    // Only a device that rotates (a tablet or 2-in-1) honours this; elsewhere Windows accepts it and nothing turns.
    // Each choice includes its flipped variant, so the device can still turn upside down within it.
    protected override Task<bool> ApplyOrientation(DeviceOrientation orientation)
    {
        var preference = orientation switch
        {
            DeviceOrientation.Portrait => OrientationPreferencePortrait | OrientationPreferencePortraitFlipped,
            DeviceOrientation.Landscape => OrientationPreferenceLandscape | OrientationPreferenceLandscapeFlipped,
            _ => OrientationPreferenceNone
        };
        return Task.FromResult(SetDisplayAutoRotationPreferences(preference));
    }


    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DevMode devMode);

    [DllImport("user32.dll")]
    static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    static extern bool SetDisplayAutoRotationPreferences(int orientation);


    // DEVMODEW, display variant of the union
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct DevMode
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }
}
