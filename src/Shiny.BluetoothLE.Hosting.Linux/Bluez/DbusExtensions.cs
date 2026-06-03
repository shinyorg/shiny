using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting.Bluez;


internal static class DbusExtensions
{
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


    public static bool ReadBoolVariant(this Reader reader)
    {
        reader.ReadSignature();
        return reader.ReadBool();
    }
}
