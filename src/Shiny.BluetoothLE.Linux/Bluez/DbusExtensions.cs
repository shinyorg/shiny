using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal static class DbusExtensions
{
    // Tmds.DBus.Protocol's CallMethodAsync takes no CancellationToken, so cancelling stops the
    // caller waiting and the reply is dropped - BlueZ still runs the call once it is sent. A token
    // that is already cancelled sends nothing. These are named CallAsync rather than overloading
    // CallMethodAsync: its generic form has an optional object readerState, which would quietly
    // bind a CancellationToken and never cancel.
    public static Task CallAsync(this DBusConnection connection, MessageBuffer message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return connection.CallMethodAsync(message).WaitAsync(ct);
    }


    public static Task<T> CallAsync<T>(this DBusConnection connection, MessageBuffer message, MessageValueReader<T> reader, CancellationToken ct, object? readerState = null)
    {
        ct.ThrowIfCancellationRequested();
        return connection.CallMethodAsync(message, reader, readerState).WaitAsync(ct);
    }


    public static MessageBuffer CreateMethodCall(
        this DBusConnection connection,
        string destination,
        string path,
        string @interface,
        string method,
        string? signature = null)
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: destination,
            path: path,
            @interface: @interface,
            member: method,
            signature: signature
        );
        return writer.CreateMessage();
    }


    public static MessageBuffer CreateGetPropertyCall(
        this DBusConnection connection,
        string destination,
        string path,
        string @interface,
        string property)
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: destination,
            path: path,
            @interface: BluezConstants.PropertiesInterface,
            member: "Get",
            signature: "ss"
        );
        writer.WriteString(@interface);
        writer.WriteString(property);
        return writer.CreateMessage();
    }


    /// <summary>
    /// Skips a variant whose value is not needed - any property the caller does not handle.
    /// </summary>
    /// <remarks>
    /// <see cref="Reader.ReadVariantValue()"/> reads the variant's own signature, so unlike the
    /// typed readers below it must not be preceded by <see cref="Reader.ReadSignature"/>.
    /// <para>
    /// Every helper here takes the reader by <c>ref</c>. <see cref="Reader"/> is a ref struct, so
    /// a helper taking it by value advances a copy and leaves the caller where it was - the next
    /// read then takes a property's value for its name, and the parse throws
    /// <c>DBusReadException: Invalid variant signature</c> on the first device BlueZ already knows.
    /// </para>
    /// </remarks>
    public static void SkipVariant(this ref Reader reader) => reader.ReadVariantValue();


    public static string? ReadStringVariant(this ref Reader reader)
    {
        reader.ReadSignature(); // variant signature
        return reader.ReadString();
    }


    public static bool ReadBoolVariant(this ref Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadBool();
    }


    public static short ReadInt16Variant(this ref Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadInt16();
    }


    public static string[] ReadStringArrayVariant(this ref Reader reader)
    {
        reader.ReadSignature();
        var list = new List<string>();
        var arrayEnd = reader.ReadArrayStart(DBusType.String);
        while (reader.HasNext(arrayEnd))
        {
            list.Add(reader.ReadString());
        }
        return list.ToArray();
    }


    public static byte[] ReadByteArrayVariant(this ref Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadArrayOfByte();
    }


    public static Dictionary<ushort, byte[]> ReadManufacturerDataVariant(this ref Reader reader)
    {
        var result = new Dictionary<ushort, byte[]>();
        reader.ReadSignature(); // a{qv}
        var arrayEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(arrayEnd))
        {
            var key = reader.ReadUInt16();
            reader.ReadSignature(); // variant sig
            var value = reader.ReadArrayOfByte();
            result[key] = value;
        }
        return result;
    }


    public static Dictionary<string, byte[]> ReadServiceDataVariant(this ref Reader reader)
    {
        var result = new Dictionary<string, byte[]>();
        reader.ReadSignature(); // a{sv}
        var arrayEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(arrayEnd))
        {
            var key = reader.ReadString();
            reader.ReadSignature(); // variant sig
            var value = reader.ReadArrayOfByte();
            result[key] = value;
        }
        return result;
    }
}
