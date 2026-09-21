using System.Runtime.InteropServices;

namespace Shiny.Gamepad.Evdev;


/// <summary>One event from an evdev device node.</summary>
/// <remarks>
/// <para>The timestamp is a <c>struct timeval</c>, whose members are the platform's <c>time_t</c>
/// and <c>suseconds_t</c> - 64-bit on x86-64 and arm64, making this struct 24 bytes. Declaring them
/// as <see cref="nint"/> rather than <see cref="long"/> keeps the layout right on a 32-bit build
/// too, where the same struct is 16 bytes and a fixed 24 would read every event misaligned.</para>
/// <para>The timestamp itself is not used - <see cref="Environment.TickCount64"/> is, so timestamps
/// are comparable with the other backends' - but the field has to be there for the rest to land in
/// the right place.</para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
struct InputEvent
{
    public nint Seconds;
    public nint Microseconds;
    public ushort Type;
    public ushort Code;
    public int Value;
}


/// <summary>A device's bus, vendor, product and version, from <c>EVIOCGID</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
struct InputId
{
    public ushort BusType;
    public ushort Vendor;
    public ushort Product;
    public ushort Version;
}


/// <summary>An axis's range, resting slop and resolution, from <c>EVIOCGABS</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
struct InputAbsInfo
{
    public int Value;
    public int Minimum;
    public int Maximum;
    public int Fuzz;
    public int Flat;
    public int Resolution;
}


/// <summary>
/// A force-feedback effect, laid out exactly as <c>struct ff_effect</c>.
/// </summary>
/// <remarks>
/// <para><b>The offsets are explicit because the natural C# layout is wrong.</b> The real struct
/// ends in a union of five effect types, the largest of which (<c>ff_periodic_effect</c>) contains
/// a pointer - so the union is aligned to 8 on a 64-bit kernel and begins at offset 16, two bytes
/// past where the preceding fields end. Laid out sequentially the magnitudes land at 14 and the
/// struct comes out 56 bytes, and since <c>EVIOCSFF</c> encodes its size in the request number,
/// the kernel would reject the ioctl outright - or, worse, accept a request built from the right
/// size and read the magnitudes from the wrong place.</para>
/// <para>Sized for LP64 (x86-64 and arm64), where <c>sizeof(struct ff_effect)</c> is 48. On a
/// 32-bit kernel the union's pointer shrinks and the struct is 44; the magnitudes are still at 16,
/// so the same buffer works and only the size passed to the ioctl changes - see
/// <see cref="EvdevNative.SetForceFeedback"/>.</para>
/// <para><see cref="Id"/> is set to -1 to upload a new effect; the kernel writes the allocated id
/// back into it, and that id is what gets played and later removed.</para>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 48)]
struct FFEffect
{
    [FieldOffset(0)] public ushort Type;
    [FieldOffset(2)] public short Id;
    [FieldOffset(4)] public ushort Direction;

    // struct ff_trigger
    [FieldOffset(6)] public ushort TriggerButton;
    [FieldOffset(8)] public ushort TriggerInterval;

    // struct ff_replay
    [FieldOffset(10)] public ushort ReplayLength;
    [FieldOffset(12)] public ushort ReplayDelay;

    // the union begins here, 8-byte aligned - struct ff_rumble_effect is its first two fields
    [FieldOffset(16)] public ushort StrongMagnitude;
    [FieldOffset(18)] public ushort WeakMagnitude;
}


/// <summary>One entry of the array <c>poll(2)</c> is given.</summary>
[StructLayout(LayoutKind.Sequential)]
struct PollFd
{
    public int Fd;
    public short Events;
    public short Revents;
}


/// <summary>
/// The libc calls evdev needs.
/// </summary>
/// <remarks>
/// evdev has no library to link against - it is a character device driven by <c>read</c>,
/// <c>write</c> and a set of ioctls, and <c>libevdev</c> is a convenience wrapper over exactly
/// these. Calling them directly keeps the package dependency-free and AOT-clean, at the cost of
/// having to encode the ioctl request numbers by hand.
/// </remarks>
static unsafe partial class EvdevNative
{
    const string Libc = "libc";

    [LibraryImport(Libc, EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    internal static partial int Open(string pathname, int flags);

    [LibraryImport(Libc, EntryPoint = "close", SetLastError = true)]
    internal static partial int Close(int fd);

    [LibraryImport(Libc, EntryPoint = "read", SetLastError = true)]
    internal static partial nint Read(int fd, void* buffer, nuint count);

    [LibraryImport(Libc, EntryPoint = "write", SetLastError = true)]
    internal static partial nint Write(int fd, void* buffer, nuint count);

    [LibraryImport(Libc, EntryPoint = "ioctl", SetLastError = true)]
    internal static partial int Ioctl(int fd, nuint request, void* argument);

    [LibraryImport(Libc, EntryPoint = "pipe", SetLastError = true)]
    internal static partial int Pipe(int* fds);

    [LibraryImport(Libc, EntryPoint = "poll", SetLastError = true)]
    internal static partial int Poll(PollFd* fds, nuint count, int timeoutMilliseconds);


    // _IOC direction bits, from asm-generic/ioctl.h - the encoding every architecture .NET runs on
    // under Linux shares
    const nuint IocWrite = 1;
    const nuint IocRead = 2;
    const int IocNrBits = 8;
    const int IocTypeBits = 8;
    const int IocSizeBits = 14;
    const int IocNrShift = 0;
    const int IocTypeShift = IocNrShift + IocNrBits;
    const int IocSizeShift = IocTypeShift + IocTypeBits;
    const int IocDirShift = IocSizeShift + IocSizeBits;

    const nuint EvdevType = 'E';


    static nuint Ioc(nuint direction, nuint number, nuint size)
        => (direction << IocDirShift) | (EvdevType << IocTypeShift) | (number << IocNrShift) | (size << IocSizeShift);


    /// <summary>The device's name, as <c>EVIOCGNAME(len)</c>.</summary>
    internal static nuint GetName(nuint length) => Ioc(IocRead, 0x06, length);

    /// <summary>The device's physical location, as <c>EVIOCGPHYS(len)</c>.</summary>
    internal static nuint GetPhys(nuint length) => Ioc(IocRead, 0x07, length);

    /// <summary>The device's unique identifier - usually a MAC address - as <c>EVIOCGUNIQ(len)</c>.</summary>
    internal static nuint GetUniq(nuint length) => Ioc(IocRead, 0x08, length);

    /// <summary>The device's property bits, as <c>EVIOCGPROP(len)</c>.</summary>
    internal static nuint GetProperties(nuint length) => Ioc(IocRead, 0x09, length);

    /// <summary>The device's bus, vendor and product ids, as <c>EVIOCGID</c>.</summary>
    internal static nuint GetId() => Ioc(IocRead, 0x02, (nuint)sizeof(InputId));

    /// <summary>Which codes of one event type the device supports, as <c>EVIOCGBIT(type, len)</c>.</summary>
    internal static nuint GetBits(ushort eventType, nuint length) => Ioc(IocRead, (nuint)(0x20 + eventType), length);

    /// <summary>One axis's range, as <c>EVIOCGABS(axis)</c>.</summary>
    internal static nuint GetAbsInfo(ushort axis) => Ioc(IocRead, (nuint)(0x40 + axis), (nuint)sizeof(InputAbsInfo));

    /// <summary>
    /// Uploads or replaces a force-feedback effect, as <c>EVIOCSFF</c>.
    /// </summary>
    /// <remarks>
    /// The size is the kernel's <c>sizeof(struct ff_effect)</c>, which differs by pointer width
    /// because the effect union holds one: 48 on LP64, 44 on a 32-bit kernel. It cannot come from
    /// <c>sizeof(FFEffect)</c>, which is pinned at the larger of the two so one buffer serves both.
    /// </remarks>
    internal static nuint SetForceFeedback() => Ioc(IocWrite, 0x80, IntPtr.Size == 8 ? 48u : 44u);

    /// <summary>
    /// Removes an uploaded effect, as <c>EVIOCRMFF</c>.
    /// </summary>
    /// <remarks>
    /// Declared <c>_IOW(..., int)</c>, but the effect id is passed <i>by value</i> in the argument
    /// slot rather than through a pointer - the only ioctl here that does.
    /// </remarks>
    internal static nuint RemoveForceFeedback() => Ioc(IocWrite, 0x81, sizeof(int));


    /// <summary>Whether a bit is set in a bitmap returned by one of the <c>EVIOC*BIT</c> ioctls.</summary>
    internal static bool HasBit(ReadOnlySpan<byte> bitmap, int bit)
    {
        var index = bit / 8;

        return index < bitmap.Length && (bitmap[index] & (1 << (bit % 8))) != 0;
    }
}
