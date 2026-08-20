using System.Net;
using System.Text;
using Tmds.DBus.Protocol;
using MessageNotification = Tmds.DBus.Protocol.Notification<Tmds.DBus.Protocol.Message>;

namespace Shiny.Net.Wifi.NetworkManager;


/// <summary>An access point as NetworkManager describes it.</summary>
internal sealed record NmAccessPoint(
    string Path,
    string Ssid,
    string? Bssid,
    byte Strength,
    uint FrequencyMhz,
    WifiSecurity Security
);


/// <summary>The addressing NetworkManager assigned to a device.</summary>
internal sealed record NmIpConfig(
    IReadOnlyList<IPAddress> Addresses,
    IReadOnlyList<IPAddress> Nameservers,
    IPAddress? Gateway,
    int PrefixLength
);


/// <summary>
/// A thin client over the NetworkManager D-Bus API on the system bus.
/// </summary>
/// <remarks>
/// Only the Wi-Fi corner of NetworkManager is covered: the manager object, the first wireless
/// device, its access points, its IPv4 configuration and the activate/deactivate calls.
/// </remarks>
internal sealed class NmClient : IAsyncDisposable
{
    DBusConnection? connection;
    string? wifiDevicePath;


    public async Task<DBusConnection> GetConnection(CancellationToken ct = default)
    {
        if (this.connection == null)
        {
            var address = DBusAddress.System
                ?? throw new WifiException("No D-Bus system bus address is set - DBUS_SYSTEM_BUS_ADDRESS is empty and the default socket is missing");

            var created = new DBusConnection(address);
            await created.ConnectAsync().ConfigureAwait(false);
            this.connection = created;
        }
        return this.connection;
    }


    /// <summary>
    /// The object path of the first Wi-Fi device NetworkManager knows about.
    /// </summary>
    /// <remarks>
    /// Cached, because it does not change for the life of a machine unless a USB adapter is
    /// hot-plugged - and a machine with two Wi-Fi radios is rare enough that picking the first is
    /// the right default rather than a guess worth surfacing.
    /// </remarks>
    public async Task<string> GetWifiDevicePath(CancellationToken ct = default)
    {
        if (this.wifiDevicePath != null)
            return this.wifiDevicePath;

        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var devices = await conn
            .GetObjectPathArrayProperty(NmConstants.RootPath, NmConstants.ManagerInterface, "Devices")
            .ConfigureAwait(false);

        foreach (var path in devices)
        {
            var type = await conn
                .GetUInt32Property(path, NmConstants.DeviceInterface, "DeviceType")
                .ConfigureAwait(false);

            if (type == NmConstants.DeviceTypeWifi)
            {
                this.wifiDevicePath = path;
                return path;
            }
        }
        throw new WifiException("NetworkManager reports no Wi-Fi device on this machine");
    }


    public async Task<string> GetInterfaceName(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        return await conn
            .GetStringProperty(devicePath, NmConstants.DeviceInterface, "Interface")
            .ConfigureAwait(false);
    }


    public async Task RequestScan(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: devicePath,
            @interface: NmConstants.WirelessInterface,
            member: "RequestScan",
            signature: "a{sv}"
        );
        writer.WriteDictionary(new Dictionary<string, VariantValue>());

        await conn.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    public async Task<IReadOnlyList<NmAccessPoint>> GetAccessPoints(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var paths = await conn
            .GetObjectPathArrayProperty(devicePath, NmConstants.WirelessInterface, "AccessPoints")
            .ConfigureAwait(false);

        var results = new List<NmAccessPoint>(paths.Length);
        foreach (var path in paths)
        {
            var ap = await this.ReadAccessPoint(path, ct).ConfigureAwait(false);
            if (ap != null)
                results.Add(ap);
        }
        return results;
    }


    public async Task<NmAccessPoint?> ReadAccessPoint(string path, CancellationToken ct = default)
    {
        if (path == NmConstants.NullPath)
            return null;

        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        try
        {
            var ssid = await conn.GetByteArrayProperty(path, NmConstants.AccessPointInterface, "Ssid").ConfigureAwait(false);
            var strength = await conn.GetByteProperty(path, NmConstants.AccessPointInterface, "Strength").ConfigureAwait(false);
            var frequency = await conn.GetUInt32Property(path, NmConstants.AccessPointInterface, "Frequency").ConfigureAwait(false);
            var hwAddress = await conn.GetStringProperty(path, NmConstants.AccessPointInterface, "HwAddress").ConfigureAwait(false);
            var flags = await conn.GetUInt32Property(path, NmConstants.AccessPointInterface, "Flags").ConfigureAwait(false);
            var wpaFlags = await conn.GetUInt32Property(path, NmConstants.AccessPointInterface, "WpaFlags").ConfigureAwait(false);
            var rsnFlags = await conn.GetUInt32Property(path, NmConstants.AccessPointInterface, "RsnFlags").ConfigureAwait(false);

            return new NmAccessPoint(
                path,
                // NetworkManager carries the SSID as raw bytes because 802.11 does not require it
                // to be text; UTF-8 is what everything in practice uses
                Encoding.UTF8.GetString(ssid),
                String.IsNullOrEmpty(hwAddress) ? null : hwAddress,
                strength,
                frequency,
                ToSecurity((NmApFlags)flags, (NmApSecurity)wpaFlags, (NmApSecurity)rsnFlags)
            );
        }
        catch (DBusErrorReplyException)
        {
            // an access point that aged out between listing and reading - it is gone, not an error
            return null;
        }
    }


    /// <remarks>
    /// NetworkManager reports the beacon flags and the WPA and RSN information elements separately.
    /// RSN is the WPA2/WPA3 element, so it is checked first: a network in a WPA2/WPA3 transition
    /// mode populates both, and reporting the WPA half would understate it.
    /// </remarks>
    public static WifiSecurity ToSecurity(NmApFlags flags, NmApSecurity wpa, NmApSecurity rsn)
    {
        if (rsn.HasFlag(NmApSecurity.KeyMgmtSae))
            return WifiSecurity.Wpa3Psk;

        if (rsn.HasFlag(NmApSecurity.KeyMgmt8021X) ||
            rsn.HasFlag(NmApSecurity.KeyMgmtEapSuiteB192) ||
            wpa.HasFlag(NmApSecurity.KeyMgmt8021X))
            return WifiSecurity.Enterprise;

        if (rsn.HasFlag(NmApSecurity.KeyMgmtOwe) || rsn.HasFlag(NmApSecurity.KeyMgmtOweTransition))
            return WifiSecurity.Owe;

        if (rsn.HasFlag(NmApSecurity.KeyMgmtPsk))
            return WifiSecurity.Wpa2Psk;

        if (wpa.HasFlag(NmApSecurity.KeyMgmtPsk))
            return WifiSecurity.WpaPsk;

        // privacy with neither a WPA nor an RSN element left is WEP by elimination
        if (flags.HasFlag(NmApFlags.Privacy) && wpa == NmApSecurity.None && rsn == NmApSecurity.None)
            return WifiSecurity.Wep;

        return WifiSecurity.Open;
    }


    public async Task<NmAccessPoint?> GetActiveAccessPoint(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var path = await conn
            .GetObjectPathProperty(devicePath, NmConstants.WirelessInterface, "ActiveAccessPoint")
            .ConfigureAwait(false);

        return await this.ReadAccessPoint(path, ct).ConfigureAwait(false);
    }


    public async Task<NmIpConfig?> GetIp4Config(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var path = await conn
            .GetObjectPathProperty(devicePath, NmConstants.DeviceInterface, "Ip4Config")
            .ConfigureAwait(false);

        if (path == NmConstants.NullPath)
            return null;

        var addressData = await conn.GetDictArrayProperty(path, NmConstants.Ip4ConfigInterface, "AddressData").ConfigureAwait(false);
        var nameserverData = await conn.GetDictArrayProperty(path, NmConstants.Ip4ConfigInterface, "NameserverData").ConfigureAwait(false);
        var gateway = await conn.GetStringProperty(path, NmConstants.Ip4ConfigInterface, "Gateway").ConfigureAwait(false);

        return new NmIpConfig(
            addressData.Select(x => ReadAddress(x, "address")).Where(x => x != null).ToArray()!,
            nameserverData.Select(x => ReadAddress(x, "address")).Where(x => x != null).ToArray()!,
            String.IsNullOrEmpty(gateway) ? null : IPAddress.Parse(gateway),
            addressData.Count == 0 ? 0 : (int)ReadUInt(addressData[0], "prefix")
        );
    }


    static IPAddress? ReadAddress(Dictionary<string, VariantValue> entry, string key)
        => entry.TryGetValue(key, out var value) && IPAddress.TryParse(value.GetString(), out var parsed)
            ? parsed
            : null;


    static uint ReadUInt(Dictionary<string, VariantValue> entry, string key)
        => entry.TryGetValue(key, out var value) ? value.GetUInt32() : 0;


    /// <summary>
    /// Adds a connection profile and brings it up on a device, returning the active connection path.
    /// </summary>
    /// <param name="volatile">
    /// Ask NetworkManager to forget the profile once it goes down. Uses AddAndActivateConnection2,
    /// which arrived in NetworkManager 1.16 - older daemons fall back to the persisting call.
    /// </param>
    public async Task<string> AddAndActivate(
        NmConnectionSettings settings,
        string devicePath,
        string specificObject,
        bool @volatile,
        CancellationToken ct = default
    )
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);

        if (@volatile)
        {
            try
            {
                return await this.AddAndActivate2(conn, settings, devicePath, specificObject).ConfigureAwait(false);
            }
            catch (DBusErrorReplyException)
            {
                // pre-1.16 daemon: the profile will be saved, which is the documented fallback
            }
        }

        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.RootPath,
            @interface: NmConstants.ManagerInterface,
            member: "AddAndActivateConnection",
            signature: "a{sa{sv}}oo"
        );
        writer.WriteConnectionSettings(settings);
        writer.WriteObjectPath(devicePath);
        writer.WriteObjectPath(specificObject);

        return await conn.CallMethodAsync(
            writer.CreateMessage(),
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                reader.ReadObjectPathAsString();          // the saved profile
                return reader.ReadObjectPathAsString();   // the active connection
            }
        ).ConfigureAwait(false);
    }


    async Task<string> AddAndActivate2(DBusConnection conn, NmConnectionSettings settings, string devicePath, string specificObject)
    {
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.RootPath,
            @interface: NmConstants.ManagerInterface,
            member: "AddAndActivateConnection2",
            signature: "a{sa{sv}}ooa{sv}"
        );
        writer.WriteConnectionSettings(settings);
        writer.WriteObjectPath(devicePath);
        writer.WriteObjectPath(specificObject);
        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["persist"] = VariantValue.String("volatile")
        });

        return await conn.CallMethodAsync(
            writer.CreateMessage(),
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                reader.ReadObjectPathAsString();
                return reader.ReadObjectPathAsString();
            }
        ).ConfigureAwait(false);
    }


    public async Task Deactivate(string activeConnectionPath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.RootPath,
            @interface: NmConstants.ManagerInterface,
            member: "DeactivateConnection",
            signature: "o"
        );
        writer.WriteObjectPath(activeConnectionPath);

        await conn.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    public async Task<string> GetActiveConnection(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        return await conn
            .GetObjectPathProperty(devicePath, NmConstants.DeviceInterface, "ActiveConnection")
            .ConfigureAwait(false);
    }


    public async Task<uint> GetDeviceState(string devicePath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        return await conn
            .GetUInt32Property(devicePath, NmConstants.DeviceInterface, "State")
            .ConfigureAwait(false);
    }


    public async Task<bool> GetWirelessEnabled(CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        return await conn
            .GetBoolProperty(NmConstants.RootPath, NmConstants.ManagerInterface, "WirelessEnabled")
            .ConfigureAwait(false);
    }


    public async Task SetWirelessEnabled(bool enabled, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        await conn
            .SetBoolProperty(NmConstants.RootPath, NmConstants.ManagerInterface, "WirelessEnabled", enabled)
            .ConfigureAwait(false);
    }


    public async Task<bool> GetWwanEnabled(CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        return await conn
            .GetBoolProperty(NmConstants.RootPath, NmConstants.ManagerInterface, "WwanEnabled")
            .ConfigureAwait(false);
    }


    public async Task SetWwanEnabled(bool enabled, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        await conn
            .SetBoolProperty(NmConstants.RootPath, NmConstants.ManagerInterface, "WwanEnabled", enabled)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Watches every PropertiesChanged signal NetworkManager emits.
    /// </summary>
    /// <remarks>
    /// Deliberately unfiltered by path. Wi-Fi state moves across the manager, the device and the
    /// IP4Config objects, and the IP4Config path itself changes on every new lease - so matching on
    /// a path would miss exactly the changes worth hearing about. The caller de-duplicates.
    /// </remarks>
    public async Task<IDisposable> WatchPropertiesChanged(Action onChanged, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = NmConstants.Service,
            Interface = NmConstants.PropertiesInterface,
            Member = "PropertiesChanged"
        };

        return await conn.AddMatchAsync(
            rule,
            static (Message msg, object? _) => msg,
            static (MessageNotification n) =>
            {
                if (n.Exception == null)
                    ((Action)n.State!).Invoke();
            },
            emitOnCapturedContext: false,
            ObserverFlags.None,
            onChanged
        ).ConfigureAwait(false);
    }


    public ValueTask DisposeAsync()
    {
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }
}
