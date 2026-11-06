using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Shiny.BluetoothLE.McuMgr.Tests;


static class TestImages
{
    /// <summary>
    /// Builds a minimal signed MCUboot image: 32 byte header, body, optional protected TLVs, then the TLV info
    /// area with a SHA-256 entry (and a dummy key hash entry before it).
    /// </summary>
    public static byte[] Create(int bodySize, McuBootVersion version, int seed = 1, bool withProtectedTlv = false)
    {
        var body = new byte[bodySize];
        new Random(seed).NextBytes(body);

        using var ms = new MemoryStream();
        var header = new byte[32];
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x96f3b83d);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(8), 32);

        var protectedTlv = Array.Empty<byte>();
        if (withProtectedTlv)
        {
            // info header + one security counter TLV (0x50, 4 bytes)
            protectedTlv = new byte[4 + 4 + 4];
            BinaryPrimitives.WriteUInt16LittleEndian(protectedTlv, 0x6908);
            BinaryPrimitives.WriteUInt16LittleEndian(protectedTlv.AsSpan(2), (ushort)protectedTlv.Length);
            BinaryPrimitives.WriteUInt16LittleEndian(protectedTlv.AsSpan(4), 0x50);
            BinaryPrimitives.WriteUInt16LittleEndian(protectedTlv.AsSpan(6), 4);
        }
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(10), (ushort)protectedTlv.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(12), (uint)bodySize);
        header[20] = version.Major;
        header[21] = version.Minor;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(22), version.Revision);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(24), version.Build);

        ms.Write(header);
        ms.Write(body);
        ms.Write(protectedTlv);

        var hash = SHA256.HashData(ms.ToArray());
        var keyHash = new byte[32];

        var tlv = new byte[4 + (4 + 32) + (4 + 32)];
        BinaryPrimitives.WriteUInt16LittleEndian(tlv, 0x6907);
        BinaryPrimitives.WriteUInt16LittleEndian(tlv.AsSpan(2), (ushort)tlv.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(tlv.AsSpan(4), 0x01); // key hash
        BinaryPrimitives.WriteUInt16LittleEndian(tlv.AsSpan(6), 32);
        keyHash.CopyTo(tlv, 8);
        BinaryPrimitives.WriteUInt16LittleEndian(tlv.AsSpan(40), 0x10); // sha256
        BinaryPrimitives.WriteUInt16LittleEndian(tlv.AsSpan(42), 32);
        hash.CopyTo(tlv, 44);
        ms.Write(tlv);

        return ms.ToArray();
    }


    public static byte[] CreateDfuZip(params (string File, string ImageIndex, byte[] Data, string Type)[] files)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entries = String.Join(",", files.Select(x =>
                $$"""{"type":"{{x.Type}}","board":"nrf5340dk","soc":"nrf5340","load_address":0,"image_index":"{{x.ImageIndex}}","slot_index_primary":"1","slot_index_secondary":"2","version_MCUBOOT":"1.0.0","size":{{x.Data.Length}},"file":"{{x.File}}","modtime":0,"version":"1.0.0"}"""
            ));
            var manifest = $$"""{"format-version":0,"time":0,"files":[{{entries}}],"name":"dfu"}""";

            using (var writer = new StreamWriter(zip.CreateEntry("manifest.json").Open(), Encoding.UTF8))
                writer.Write(manifest);

            foreach (var file in files)
            {
                using var stream = zip.CreateEntry(file.File).Open();
                stream.Write(file.Data);
            }
        }
        return ms.ToArray();
    }
}
