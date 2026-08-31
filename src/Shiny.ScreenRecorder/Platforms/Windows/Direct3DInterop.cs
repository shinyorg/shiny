using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;

namespace Shiny.ScreenRecorder;


/// <summary>
/// The native calls that produce the <see cref="IDirect3DDevice"/> Windows.Graphics.Capture needs.
/// </summary>
/// <remarks>
/// <para>There is no managed way to make one. <c>Direct3D11CaptureFramePool.CreateFreeThreaded</c>
/// requires an <c>IDirect3DDevice</c>, and the only thing that produces one is
/// <c>CreateDirect3D11DeviceFromDXGIDevice</c> in d3d11.dll - which is not part of the WinRT
/// projection, so CsWinRT cannot hand it over.</para>
/// <para>The QueryInterface and Release calls go through the vtable by hand rather than through
/// <c>ComImport</c>. Built-in COM interop is not trim- or AOT-safe, and this project ships with
/// <c>IsAotCompatible</c> set; three unmanaged function-pointer calls keep that promise where a
/// <c>ComImport</c> interface would break it.</para>
/// </remarks>
internal static unsafe partial class Direct3DInterop
{
    static readonly Guid IID_IDXGIDevice = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");

    // D3D_DRIVER_TYPE_HARDWARE
    const uint DriverTypeHardware = 1;

    // D3D11_CREATE_DEVICE_BGRA_SUPPORT - required for interop with WinRT/Direct2D surfaces
    const uint CreateDeviceBgraSupport = 0x20;


    [LibraryImport("d3d11.dll")]
    private static partial int D3D11CreateDevice(
        IntPtr adapter,
        uint driverType,
        IntPtr software,
        uint flags,
        IntPtr featureLevels,
        uint featureLevelCount,
        uint sdkVersion,
        out IntPtr device,
        out uint featureLevel,
        out IntPtr immediateContext
    );


    [LibraryImport("d3d11.dll")]
    private static partial int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);


    /// <summary>
    /// Creates a hardware D3D11 device and projects it as the WinRT <see cref="IDirect3DDevice"/>
    /// the capture APIs take.
    /// </summary>
    public static IDirect3DDevice CreateDevice()
    {
        // D3D11_SDK_VERSION is 7 and has been since D3D11 shipped
        var hr = D3D11CreateDevice(
            IntPtr.Zero,
            DriverTypeHardware,
            IntPtr.Zero,
            CreateDeviceBgraSupport,
            IntPtr.Zero,
            0,
            7,
            out var device,
            out _,
            out var context
        );

        if (hr < 0 || device == IntPtr.Zero)
            throw new ScreenRecorderException($"Could not create a Direct3D 11 device (HRESULT 0x{hr:X8})");

        // the immediate context is not used - frames go straight from the capture pool to the
        // encoder - so it is released rather than leaked
        if (context != IntPtr.Zero)
            Release(context);

        IntPtr dxgiDevice = IntPtr.Zero;
        IntPtr inspectable = IntPtr.Zero;
        try
        {
            var iid = IID_IDXGIDevice;
            hr = QueryInterface(device, &iid, out dxgiDevice);
            if (hr < 0 || dxgiDevice == IntPtr.Zero)
                throw new ScreenRecorderException($"The Direct3D device does not expose IDXGIDevice (HRESULT 0x{hr:X8})");

            hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out inspectable);
            if (hr < 0 || inspectable == IntPtr.Zero)
                throw new ScreenRecorderException($"Could not project the Direct3D device for WinRT (HRESULT 0x{hr:X8})");

            // FromAbi takes a reference of its own, so the local one is still ours to release
            return WinRT.MarshalInspectable<IDirect3DDevice>.FromAbi(inspectable);
        }
        finally
        {
            if (inspectable != IntPtr.Zero)
                Release(inspectable);

            if (dxgiDevice != IntPtr.Zero)
                Release(dxgiDevice);

            Release(device);
        }
    }


    // IUnknown::QueryInterface is vtable slot 0
    static int QueryInterface(IntPtr unknown, Guid* iid, out IntPtr result)
    {
        var vtable = *(void***)unknown;
        var queryInterface = (delegate* unmanaged[Stdcall]<IntPtr, Guid*, out IntPtr, int>)vtable[0];

        return queryInterface(unknown, iid, out result);
    }


    // IUnknown::Release is vtable slot 2
    static uint Release(IntPtr unknown)
    {
        var vtable = *(void***)unknown;
        var release = (delegate* unmanaged[Stdcall]<IntPtr, uint>)vtable[2];

        return release(unknown);
    }
}
