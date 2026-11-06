using System.Buffers.Binary;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// An MCUboot image version (<c>major.minor.revision+build</c>).
/// </summary>
public readonly record struct McuBootVersion(byte Major, byte Minor, ushort Revision, uint Build)
{
    /// <summary>
    /// Formats the version the way the device reports it in image state - <c>1.2.3</c>, or <c>1.2.3.4</c> when
    /// the build number is not zero.
    /// </summary>
    public override string ToString() => this.Build == 0
        ? $"{this.Major}.{this.Minor}.{this.Revision}"
        : $"{this.Major}.{this.Minor}.{this.Revision}.{this.Build}";
}


/// <summary>
/// The header and hash of a signed MCUboot image, read without the device. The <see cref="Hash"/> is what
/// image state reports and what test/confirm requests identify the image by.
/// </summary>
public sealed class McuBootImage
{
    const uint ImageMagic = 0x96f3b83d;
    const ushort TlvInfoMagic = 0x6907;
    const ushort TlvProtectedInfoMagic = 0x6908;
    const ushort TlvSha256 = 0x10;
    const ushort TlvSha384 = 0x11;
    const ushort TlvSha512 = 0x12;

    McuBootImage(uint loadAddress, ushort headerSize, uint imageSize, uint flags, McuBootVersion version, byte[] hash)
    {
        this.LoadAddress = loadAddress;
        this.HeaderSize = headerSize;
        this.ImageSize = imageSize;
        this.Flags = flags;
        this.Version = version;
        this.Hash = hash;
    }


    /// <summary>The load address (0 for images that run in place).</summary>
    public uint LoadAddress { get; }

    /// <summary>The header size in bytes.</summary>
    public ushort HeaderSize { get; }

    /// <summary>The size of the image body in bytes (excluding header and TLVs).</summary>
    public uint ImageSize { get; }

    /// <summary>The image flags.</summary>
    public uint Flags { get; }

    /// <summary>The image version.</summary>
    public McuBootVersion Version { get; }

    /// <summary>The image hash from the SHA-256 (or SHA-384/512) TLV.</summary>
    public byte[] Hash { get; }

    /// <summary>The hash as lower case hex.</summary>
    public string HashHex => Convert.ToHexStringLower(this.Hash);


    /// <summary>
    /// Returns true when <paramref name="data"/> starts with the MCUboot image magic.
    /// </summary>
    public static bool IsMcuBootImage(ReadOnlySpan<byte> data)
        => data.Length >= 4 && BinaryPrimitives.ReadUInt32LittleEndian(data) == ImageMagic;


    /// <summary>
    /// Parses a signed MCUboot image.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a signed MCUboot image or has no hash TLV.</exception>
    public static McuBootImage Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32 || !IsMcuBootImage(data))
            throw new InvalidDataException("Not an MCUboot image - use the signed binary (e.g. zephyr.signed.bin or app_update.bin), not zephyr.bin or a hex file");

        var loadAddress = BinaryPrimitives.ReadUInt32LittleEndian(data[4..]);
        var headerSize = BinaryPrimitives.ReadUInt16LittleEndian(data[8..]);
        var protectedTlvSize = BinaryPrimitives.ReadUInt16LittleEndian(data[10..]);
        var imageSize = BinaryPrimitives.ReadUInt32LittleEndian(data[12..]);
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(data[16..]);
        var version = new McuBootVersion(
            data[20],
            data[21],
            BinaryPrimitives.ReadUInt16LittleEndian(data[22..]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[24..])
        );

        var offset = (long)headerSize + imageSize;
        byte[]? hash = null;

        if (protectedTlvSize > 0)
        {
            hash = FindHash(data, offset, TlvProtectedInfoMagic);
            offset += protectedTlvSize;
        }
        hash ??= FindHash(data, offset, TlvInfoMagic);

        if (hash == null)
            throw new InvalidDataException("The MCUboot image has no SHA TLV - is it signed?");

        return new McuBootImage(loadAddress, headerSize, imageSize, flags, version, hash);
    }


    static byte[]? FindHash(ReadOnlySpan<byte> data, long start, ushort magic)
    {
        if (start + 4 > data.Length)
            throw new InvalidDataException("The MCUboot image is truncated before its TLV area");

        var area = data[(int)start..];
        if (BinaryPrimitives.ReadUInt16LittleEndian(area) != magic)
            throw new InvalidDataException($"The MCUboot TLV area does not start with 0x{magic:x4}");

        var total = BinaryPrimitives.ReadUInt16LittleEndian(area[2..]);
        if (total > area.Length)
            throw new InvalidDataException("The MCUboot TLV area is truncated");

        // total includes the 4 byte info header. Entries are type (u16 - u8 + pad on older images) + length (u16)
        var position = 4;
        byte[]? hash = null;
        while (hash == null && position + 4 <= total)
        {
            var type = BinaryPrimitives.ReadUInt16LittleEndian(area[position..]);
            var length = BinaryPrimitives.ReadUInt16LittleEndian(area[(position + 2)..]);
            var valueStart = position + 4;

            if (valueStart + length > total)
                throw new InvalidDataException("An MCUboot TLV runs past the end of its area");

            if (type is TlvSha256 or TlvSha384 or TlvSha512)
                hash = area.Slice(valueStart, length).ToArray();

            position = valueStart + length;
        }
        return hash;
    }
}
