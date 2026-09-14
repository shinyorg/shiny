using System.Collections.Generic;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting.Bluez;


/// <summary>
/// The plumbing every exported GATT object shares - replies and <c>org.freedesktop.DBus.Properties</c>.
/// </summary>
internal static class DbusObject
{
    /// <summary>
    /// Writes one property value as a variant, returning false (and writing nothing) for a name the object does not have.
    /// </summary>
    public delegate bool PropertyWriter(ref MessageWriter writer, string name);


    public static void ReplyEmpty(MethodContext context)
    {
        var writer = context.CreateReplyWriter("");
        try
        {
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }


    public static void ReplyIntrospect(MethodContext context, string xml)
    {
        var writer = context.CreateReplyWriter("s");
        try
        {
            writer.WriteString(xml);
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }


    /// <summary>
    /// Writes an <c>a{sv}</c> of the named properties.
    /// </summary>
    public static void WriteProperties(ref MessageWriter writer, IReadOnlyList<string> names, PropertyWriter writeOne)
    {
        var dict = writer.WriteDictionaryStart();
        foreach (var name in names)
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(name);
            writeOne(ref writer, name);
        }
        writer.WriteDictionaryEnd(dict);
    }


    public static void HandleProperties(MethodContext context, string interfaceName, IReadOnlyList<string> names, PropertyWriter writeOne)
    {
        switch (context.Request.MemberAsString)
        {
            case "GetAll":
            {
                var requested = ReadInterfaceName(context.Request);

                // MessageWriter is a ref struct, so it cannot be a `using` variable and passed by ref at
                // the same time - hence the explicit try/finally
                var writer = context.CreateReplyWriter("a{sv}");
                try
                {
                    // asked about an interface this object does not implement - that is no properties, not an error
                    WriteProperties(ref writer, requested == interfaceName ? names : [], writeOne);
                    context.Reply(writer.CreateMessage());
                }
                finally
                {
                    writer.Dispose();
                }
                break;
            }

            case "Get":
            {
                var (requested, name) = ReadInterfaceAndName(context.Request);
                var writer = context.CreateReplyWriter("v");
                try
                {
                    if (requested == interfaceName && writeOne(ref writer, name))
                        context.Reply(writer.CreateMessage());
                    else
                        context.ReplyError("org.freedesktop.DBus.Error.InvalidArgs", $"No such property '{name}'");
                }
                finally
                {
                    writer.Dispose();
                }
                break;
            }

            case "Set":
                context.ReplyError("org.freedesktop.DBus.Error.PropertyReadOnly", "GATT object properties are read-only");
                break;

            default:
                context.ReplyUnknownMethodError();
                break;
        }
    }


    static string ReadInterfaceName(Message request)
        => request.GetBodyReader().ReadString();


    static (string Interface, string Name) ReadInterfaceAndName(Message request)
    {
        var reader = request.GetBodyReader();
        var interfaceName = reader.ReadString();
        return (interfaceName, reader.ReadString());
    }
}
