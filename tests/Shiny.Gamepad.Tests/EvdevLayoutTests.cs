using System.Runtime.InteropServices;
using Shiny.Gamepad.Evdev;
using Xunit;

namespace Shiny.Gamepad.Tests;


/// <summary>
/// The evdev struct layouts and ioctl request numbers, which are kernel ABI.
/// </summary>
/// <remarks>
/// <para>These are the only part of the Linux backend testable without a controller, and they are
/// where the failures live. Every one of them compiles cleanly, passes on a dev machine and breaks
/// only against a real system bus - the exact shape of failure this repository has been bitten by
/// before.</para>
/// <para><c>struct ff_effect</c> is the sharpest edge. It ends in a union whose largest member holds
/// a pointer, so the union is 8-byte aligned and starts two bytes past where the preceding fields
/// end. A sequential C# layout puts the rumble magnitudes at 14 rather than 16 and sizes the struct
/// at 56 rather than 48 - and since <c>EVIOCSFF</c> encodes the size in its request number, the
/// kernel rejects the upload or reads the magnitudes from the wrong offset. Rumble simply does
/// nothing, with no error anywhere.</para>
/// </remarks>
public class EvdevLayoutTests
{
    [Fact]
    public void FFEffectMatchesTheKernelStruct()
    {
        // sizeof(struct ff_effect) is 48 on LP64 - x86-64 and arm64, which is every Linux target
        // .NET ships for in practice
        Assert.Equal(48, Marshal.SizeOf<FFEffect>());

        // the union - and so ff_rumble_effect's two magnitudes - begins at 16, not at 14
        Assert.Equal(16, (int)Marshal.OffsetOf<FFEffect>(nameof(FFEffect.StrongMagnitude)));
        Assert.Equal(18, (int)Marshal.OffsetOf<FFEffect>(nameof(FFEffect.WeakMagnitude)));

        Assert.Equal(0, (int)Marshal.OffsetOf<FFEffect>(nameof(FFEffect.Type)));
        Assert.Equal(2, (int)Marshal.OffsetOf<FFEffect>(nameof(FFEffect.Id)));
        Assert.Equal(10, (int)Marshal.OffsetOf<FFEffect>(nameof(FFEffect.ReplayLength)));
    }


    [Fact]
    public void InputEventIsTheSizeTheKernelWrites()
    {
        // struct input_event is a timeval plus type, code and value: 24 bytes on a 64-bit kernel.
        // Get this wrong and every event after the first is read misaligned.
        var expected = (IntPtr.Size * 2) + 8;

        Assert.Equal(expected, Marshal.SizeOf<InputEvent>());
    }


    [Fact]
    public void FixedSizeStructsMatchTheirKernelCounterparts()
    {
        Assert.Equal(8, Marshal.SizeOf<InputId>());          // 4 x __u16
        Assert.Equal(24, Marshal.SizeOf<InputAbsInfo>());    // 6 x __s32
        Assert.Equal(8, Marshal.SizeOf<PollFd>());           // int + 2 x short
    }


    [Fact]
    public void LengthBearingIoctlsEncodeTheirBufferSize()
    {
        // EVIOCGNAME(len) and EVIOCGUNIQ(len) carry the caller's buffer size in the request itself,
        // so the number changes with the buffer - getting it wrong truncates the name silently
        Assert.Equal(0x81004506u, (uint)EvdevNative.GetName(256));
        Assert.Equal(0x81004508u, (uint)EvdevNative.GetUniq(256));
        Assert.Equal(0x80404506u, (uint)EvdevNative.GetName(64));
    }


    [Fact]
    public void KnownIoctlRequestNumbersAreExact()
    {
        // _IOR('E', 0x02, struct input_id)
        Assert.Equal(0x80084502u, (uint)EvdevNative.GetId());

        // _IOC(_IOC_READ, 'E', 0x20 + EV_KEY, len)
        Assert.Equal(0x80604521u, (uint)EvdevNative.GetBits(EvdevCodes.EV_KEY, 96));

        // _IOR('E', 0x40 + ABS_X, struct input_absinfo)
        Assert.Equal(0x80184540u, (uint)EvdevNative.GetAbsInfo(EvdevCodes.ABS_X));

        // _IOC(_IOC_WRITE, 'E', 0x80, sizeof(struct ff_effect))
        Assert.Equal(0x40304580u, (uint)EvdevNative.SetForceFeedback());

        // _IOW('E', 0x81, int)
        Assert.Equal(0x40044581u, (uint)EvdevNative.RemoveForceFeedback());
    }


    [Fact]
    public void BitmapsAreReadLittleEndianBitwise()
    {
        // EVIOCGBIT hands back a bitmap indexed by code: byte 0x26 bit 0 is BTN_SOUTH (0x130)
        var bitmap = new byte[96];
        bitmap[EvdevCodes.BTN_SOUTH / 8] = 1 << (EvdevCodes.BTN_SOUTH % 8);

        Assert.True(EvdevNative.HasBit(bitmap, EvdevCodes.BTN_SOUTH));
        Assert.False(EvdevNative.HasBit(bitmap, EvdevCodes.BTN_EAST));

        // a code past the end of a short bitmap must read as absent rather than overrun
        Assert.False(EvdevNative.HasBit(bitmap, EvdevCodes.KEY_MAX));
    }
}
