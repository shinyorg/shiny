using System;

namespace Shiny.BluetoothLE.Hosting;


static class UuidHelper
{
    /// <summary>The Bluetooth base UUID that 16- and 32-bit UUIDs are expanded into.</summary>
    static readonly byte[] BaseUuidTail = [0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0x80, 0x5F, 0x9B, 0x34, 0xFB];


    public static Guid ToUuid(string value)
    {
        if (value.Length == 4)
            value = $"0000{value}-0000-1000-8000-00805F9B34FB";

        return Guid.Parse(value);
    }


    /// <summary>
    /// Recovers the 16-bit short form of a UUID that sits inside the Bluetooth base UUID range,
    /// or null when the UUID is a custom 128-bit one.
    /// </summary>
    public static ushort? To16BitUuid(Guid uuid)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!uuid.TryWriteBytes(bytes, bigEndian: true, out _))
            return null;

        // 0000xxxx-0000-1000-8000-00805F9B34FB - the first two bytes and the trailing twelve are fixed
        if (bytes[0] != 0 || bytes[1] != 0)
            return null;

        if (!bytes[4..].SequenceEqual(BaseUuidTail))
            return null;

        return (ushort)((bytes[2] << 8) | bytes[3]);
    }
}
