using System;
using System.Buffers.Binary;
using System.Text;

namespace Shiny.BluetoothLE.Infrastructure;


/// <summary>
/// Op codes for the Shiny L2CAP file transfer protocol (v1).
/// </summary>
/// <remarks>
/// Wire format is a 5 byte frame header - [opcode:1][payloadLength:4 little endian] - followed
/// by the payload.  The exchange is:
/// <code>
/// initiator -> Put { size, name }   (initiator will send the file)
///           or Get { name }         (initiator wants to receive the file)
/// responder -> Accept { size, name } or Error { code, message }
/// sender    -> exactly `size` raw (unframed) bytes
/// receiver  -> Ack { bytesReceived } or Error { code, message }
/// </code>
/// The body is deliberately unframed - both sides agreed on the exact byte count in the Accept
/// frame, so per-chunk headers would only add overhead on an already MTU-constrained link.
/// </remarks>
enum L2CapOpCode : byte
{
    Put = 0x01,
    Get = 0x02,
    Accept = 0x10,
    Ack = 0x11,
    Error = 0x7F
}


/// <summary>
/// A decoded control frame.
/// </summary>
readonly record struct L2CapFrame(L2CapOpCode OpCode, byte[] Payload);


static class L2CapProtocol
{
    /// <summary>Size of the frame header in bytes.</summary>
    public const int HeaderSize = 5;

    /// <summary>Largest control frame payload we will read. Bodies are unframed so this only bounds headers.</summary>
    public const int MaxControlPayload = 64 * 1024;

    /// <summary>Largest file name (in UTF8 bytes) the protocol will carry.</summary>
    public const int MaxFileNameBytes = 1024;


    public static byte[] Frame(L2CapOpCode opCode, ReadOnlySpan<byte> payload)
    {
        var buffer = new byte[HeaderSize + payload.Length];
        buffer[0] = (byte)opCode;
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(1, 4), payload.Length);
        payload.CopyTo(buffer.AsSpan(HeaderSize));
        return buffer;
    }


    public static byte[] Put(string fileName, long size) => Frame(L2CapOpCode.Put, NameAndSize(fileName, size));
    public static byte[] Accept(string fileName, long size) => Frame(L2CapOpCode.Accept, NameAndSize(fileName, size));


    public static byte[] Get(string fileName)
    {
        var name = EncodeName(fileName);
        var payload = new byte[2 + name.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(0, 2), (ushort)name.Length);
        name.CopyTo(payload.AsSpan(2));
        return Frame(L2CapOpCode.Get, payload);
    }


    public static byte[] Ack(long bytesReceived)
    {
        var payload = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(payload, bytesReceived);
        return Frame(L2CapOpCode.Ack, payload);
    }


    public static byte[] Error(L2CapTransferError error, string? message)
    {
        var msg = Encoding.UTF8.GetBytes(message ?? String.Empty);
        if (msg.Length > MaxControlPayload - 3)
            msg = msg.AsSpan(0, MaxControlPayload - 3).ToArray();

        var payload = new byte[3 + msg.Length];
        payload[0] = (byte)error;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(1, 2), (ushort)msg.Length);
        msg.CopyTo(payload.AsSpan(3));
        return Frame(L2CapOpCode.Error, payload);
    }


    static byte[] NameAndSize(string fileName, long size)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Transfer size cannot be negative");

        var name = EncodeName(fileName);
        var payload = new byte[10 + name.Length];
        BinaryPrimitives.WriteInt64LittleEndian(payload.AsSpan(0, 8), size);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(8, 2), (ushort)name.Length);
        name.CopyTo(payload.AsSpan(10));
        return payload;
    }


    static byte[] EncodeName(string fileName)
    {
        if (String.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("A file name is required", nameof(fileName));

        var bytes = Encoding.UTF8.GetBytes(fileName);
        if (bytes.Length > MaxFileNameBytes)
            throw new ArgumentException($"File name exceeds the {MaxFileNameBytes} byte protocol limit", nameof(fileName));

        return bytes;
    }


    // -------- decoding --------

    public static (string FileName, long Size) ReadNameAndSize(this L2CapFrame frame)
    {
        var payload = frame.Payload;
        if (payload.Length < 10)
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Malformed {frame.OpCode} frame");

        var size = BinaryPrimitives.ReadInt64LittleEndian(payload.AsSpan(0, 8));
        var nameLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(8, 2));

        if (size < 0 || payload.Length < 10 + nameLen)
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Malformed {frame.OpCode} frame");

        return (Encoding.UTF8.GetString(payload, 10, nameLen), size);
    }


    public static string ReadName(this L2CapFrame frame)
    {
        var payload = frame.Payload;
        if (payload.Length < 2)
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Malformed {frame.OpCode} frame");

        var nameLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(0, 2));
        if (payload.Length < 2 + nameLen)
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, $"Malformed {frame.OpCode} frame");

        return Encoding.UTF8.GetString(payload, 2, nameLen);
    }


    public static long ReadAck(this L2CapFrame frame)
    {
        if (frame.Payload.Length < 8)
            throw new L2CapTransferException(L2CapTransferError.ProtocolError, "Malformed Ack frame");

        return BinaryPrimitives.ReadInt64LittleEndian(frame.Payload.AsSpan(0, 8));
    }


    public static L2CapTransferException ToException(this L2CapFrame frame)
    {
        var payload = frame.Payload;
        if (payload.Length < 3)
            return new L2CapTransferException(L2CapTransferError.Unknown, "Remote reported an error");

        var code = (L2CapTransferError)payload[0];
        var msgLen = BinaryPrimitives.ReadUInt16LittleEndian(payload.AsSpan(1, 2));
        var msg = payload.Length >= 3 + msgLen
            ? Encoding.UTF8.GetString(payload, 3, msgLen)
            : "Remote reported an error";

        return new L2CapTransferException(code, String.IsNullOrWhiteSpace(msg) ? code.ToString() : msg);
    }
}
