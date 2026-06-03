using System;
using System.Collections.Generic;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal static class DbusExtensions
{
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


    public static MessageBuffer CreateGetAllPropertiesCall(
        this DBusConnection connection,
        string destination,
        string path,
        string @interface)
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: destination,
            path: path,
            @interface: BluezConstants.PropertiesInterface,
            member: "GetAll",
            signature: "s"
        );
        writer.WriteString(@interface);
        return writer.CreateMessage();
    }


    public static string? ReadStringVariant(this Reader reader)
    {
        reader.ReadSignature(); // variant signature
        return reader.ReadString();
    }


    public static bool ReadBoolVariant(this Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadBool();
    }


    public static short ReadInt16Variant(this Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadInt16();
    }


    public static string[] ReadStringArrayVariant(this Reader reader)
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


    public static byte[] ReadByteArrayVariant(this Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadArrayOfByte();
    }


    public static Dictionary<ushort, byte[]> ReadManufacturerDataVariant(this Reader reader)
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


    public static Dictionary<string, byte[]> ReadServiceDataVariant(this Reader reader)
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
