using System.Buffers.Binary;
using System.Text;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// The GATT profile both halves of the BLE loop agree on. The service UUID is minted per hosting session (and
/// handed to the central over the peer channel) so a scan can never land on a stale peripheral from an earlier
/// run; the characteristic UUIDs are fixed.
/// </summary>
public static class BleTestProfile
{
    // fixed value served by the read characteristic
    public static readonly byte[] ReadValue = Encoding.UTF8.GetBytes("shiny-read");

    // the echo characteristic's value until a central writes one
    public static readonly byte[] InitialEcho = Encoding.UTF8.GetBytes("echo-empty");

    public const string LocalName = "ShinyTest";

    public const string ReadUuid = "5a1e0001-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string EchoUuid = "5a1e0002-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string WriteWithoutResponseUuid = "5a1e0003-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string ControlUuid = "5a1e0004-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string NotifyUuid = "5a1e0005-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string IndicateUuid = "5a1e0006-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string MessageUuid = "5a1e0007-0d1c-4e2b-9a7f-6b3c2d1e0f00";
    public const string PsmUuid = "5a1e0008-0d1c-4e2b-9a7f-6b3c2d1e0f00";

    public static readonly string[] AllCharacteristics =
    [
        ReadUuid,
        EchoUuid,
        WriteWithoutResponseUuid,
        ControlUuid,
        NotifyUuid,
        IndicateUuid,
        MessageUuid,
        PsmUuid
    ];

    // control characteristic commands: [command, count] - the host then sends `count` sequenced packets
    public const byte ControlNotifyBurst = 0x01;
    public const byte ControlIndicateBurst = 0x02;

    // a burst's packets are always the 20 byte minimum so they fit on any link without an MTU exchange
    public const int BurstPacketSize = 20;


    public static string NewServiceUuid() => Guid.NewGuid().ToString().ToLowerInvariant();


    // a packet carrying its sequence number in the first 4 bytes (LE), padded to size with a pattern
    public static byte[] Packet(int sequence, int size)
    {
        var data = new byte[Math.Max(size, 4)];
        BinaryPrimitives.WriteInt32LittleEndian(data, sequence);
        for (var i = 4; i < data.Length; i++)
            data[i] = (byte)(sequence + i);

        return data;
    }


    public static int ReadSequence(byte[] data)
        => data.Length < 4 ? -1 : BinaryPrimitives.ReadInt32LittleEndian(data);


    public static byte[] RandomBytes(int length)
    {
        var data = new byte[length];
        Random.Shared.NextBytes(data);
        return data;
    }


    // a read with an offset (a long read) gets the rest of the value from that offset
    public static byte[] FromOffset(byte[] value, int offset)
        => offset <= 0 ? value : offset >= value.Length ? [] : value[offset..];


    public static string Hex(byte[]? data) => data == null ? "" : Convert.ToHexString(data);
}
