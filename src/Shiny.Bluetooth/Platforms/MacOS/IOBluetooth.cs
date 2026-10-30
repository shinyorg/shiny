using System.Runtime.InteropServices;
using ObjCRuntime;

namespace Shiny.Bluetooth.MacOS;


/// <summary>
/// IOBluetooth through the Objective-C runtime. The .NET macOS SDK has no binding for the framework, so
/// every call here is an <c>objc_msgSend</c> with the exact native prototype - on arm64 a mismatched
/// prototype is undefined behaviour, so each shape gets its own import rather than a variadic one.
/// </summary>
/// <remarks>
/// BOOL is passed and returned as a byte: a signed char on x64 and a bool on arm64, both one byte.
/// IOReturn is a 32-bit kern_return_t, 0 on success.
/// </remarks>
internal static partial class IOBluetooth
{
    const string LibObjC = "/usr/lib/libobjc.dylib";
    const string FrameworkPath = "/System/Library/Frameworks/IOBluetooth.framework/IOBluetooth";

    public const int Success = 0;

    // IOBluetoothUserNotificationChannelDirection
    public const int ChannelDirectionIncoming = 1;

    static readonly Lazy<bool> loaded = new(() => Dlfcn.dlopen(FrameworkPath, 0) != IntPtr.Zero);


    /// <summary>Loads the framework (once) so its classes register with the runtime.</summary>
    public static void EnsureLoaded()
    {
        if (!loaded.Value)
            throw new BluetoothNotSupportedException("IOBluetooth.framework could not be loaded");
    }


    public static IntPtr Class(string name)
    {
        EnsureLoaded();
        var handle = ObjCRuntime.Class.GetHandle(name);
        if (handle == IntPtr.Zero)
            throw new BluetoothNotSupportedException($"IOBluetooth class {name} is not available");
        return handle;
    }


    public static IntPtr Sel(string name) => Selector.GetHandle(name);


    public static string Describe(int ioReturn) => $"IOReturn 0x{ioReturn:X8}";


    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr IntPtr_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr IntPtr_objc_msgSend_IntPtr_UInt32(IntPtr receiver, IntPtr selector, IntPtr arg1, uint arg2);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial IntPtr IntPtr_objc_msgSend_IntPtr_IntPtr_Byte_Int32(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, byte arg3, int arg4);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial byte Byte_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial sbyte SByte_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial ushort UInt16_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial uint UInt32_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend_IntPtr_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend_OutByte(IntPtr receiver, IntPtr selector, out byte arg1);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend_OutIntPtr_Byte_IntPtr(IntPtr receiver, IntPtr selector, out IntPtr arg1, byte arg2, IntPtr arg3);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial int Int32_objc_msgSend_IntPtr_UInt16_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, ushort arg2, IntPtr arg3);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial void Void_objc_msgSend(IntPtr receiver, IntPtr selector);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial void Void_objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [LibraryImport(LibObjC, EntryPoint = "objc_msgSend")]
    public static partial void Void_objc_msgSend_Byte(IntPtr receiver, IntPtr selector, byte arg1);
}
