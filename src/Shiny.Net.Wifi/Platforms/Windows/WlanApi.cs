using System.Runtime.InteropServices;

namespace Shiny.Net.Wifi.Internals;


/// <summary>
/// The slice of <c>wlanapi.dll</c> that WinRT does not cover - the saved profile store.
/// </summary>
/// <remarks>
/// <para>WiFiAdapter can scan, connect and disconnect but has no concept of a saved profile: it
/// cannot list them, delete one, or join by name. The native WLAN API is the only route, so it is
/// P/Invoked here rather than shelling out to <c>netsh</c>, which would mean parsing localised
/// console output.</para>
/// <para>Everything the API hands back is allocated by it and has to go back through
/// <c>WlanFreeMemory</c>, hence the try/finally around every call.</para>
/// </remarks>
internal static partial class WlanApi
{
    const uint SUCCESS = 0;
    const uint CLIENT_VERSION = 2;

    // WLAN_INTERFACE_INFO: GUID (16) + WCHAR[256] description (512) + WLAN_INTERFACE_STATE (4)
    const int INTERFACE_INFO_SIZE = 532;

    // WLAN_PROFILE_INFO: WCHAR[256] name (512) + DWORD flags (4)
    const int PROFILE_INFO_SIZE = 516;

    // both *_LIST structs open with dwNumberOfItems + dwIndex
    const int LIST_HEADER_SIZE = 8;

    const int NAME_LENGTH = 256;

    const uint CONNECTION_MODE_PROFILE = 0;
    const uint BSS_TYPE_ANY = 3;


    internal sealed record Profile(Guid InterfaceId, string Name, WifiSecurity Security, bool IsHidden);


    /// <summary>Every saved profile on the machine, across every Wi-Fi adapter in it.</summary>
    public static IReadOnlyList<Profile> GetProfiles()
    {
        var handle = Open();
        try
        {
            return EnumerateInterfaces(handle)
                .SelectMany(id => GetProfiles(handle, id))
                .ToList();
        }
        finally
        {
            WlanCloseHandle(handle, IntPtr.Zero);
        }
    }


    /// <summary>Deletes a saved profile. Returns false when no adapter had one by that name.</summary>
    public static bool DeleteProfile(string name)
    {
        var handle = Open();
        try
        {
            var deleted = false;
            foreach (var id in EnumerateInterfaces(handle))
            {
                var guid = id;
                if (WlanDeleteProfile(handle, in guid, name, IntPtr.Zero) == SUCCESS)
                    deleted = true;
            }
            return deleted;
        }
        finally
        {
            WlanCloseHandle(handle, IntPtr.Zero);
        }
    }


    /// <summary>
    /// Starts a join against a saved profile. Returns as soon as Windows accepts the request -
    /// association and DHCP both happen afterwards.
    /// </summary>
    public static void Connect(Guid interfaceId, string profileName)
    {
        var handle = Open();
        try
        {
            var parameters = new ConnectionParameters
            {
                Mode = CONNECTION_MODE_PROFILE,
                Profile = Marshal.StringToHGlobalUni(profileName),
                Ssid = IntPtr.Zero,
                BssidList = IntPtr.Zero,
                BssType = BSS_TYPE_ANY,
                Flags = 0
            };
            try
            {
                var result = WlanConnect(handle, in interfaceId, in parameters, IntPtr.Zero);
                if (result != SUCCESS)
                    throw new WifiConnectionException($"Windows refused to join the saved profile '{profileName}' - WLAN error {result}");
            }
            finally
            {
                Marshal.FreeHGlobal(parameters.Profile);
            }
        }
        finally
        {
            WlanCloseHandle(handle, IntPtr.Zero);
        }
    }


    static nint Open()
    {
        var result = WlanOpenHandle(CLIENT_VERSION, IntPtr.Zero, out _, out var handle);
        if (result != SUCCESS)
            throw new WifiException($"Could not open the Windows WLAN service - error {result}. It is stopped on a machine with no Wi-Fi adapter");

        return handle;
    }


    static List<Guid> EnumerateInterfaces(nint handle)
    {
        var result = WlanEnumInterfaces(handle, IntPtr.Zero, out var list);
        if (result != SUCCESS)
            throw new WifiException($"Could not enumerate Wi-Fi adapters - WLAN error {result}");

        try
        {
            var count = Marshal.ReadInt32(list);
            var ids = new List<Guid>(count);

            for (var i = 0; i < count; i++)
            {
                var entry = list + LIST_HEADER_SIZE + (i * INTERFACE_INFO_SIZE);
                ids.Add(Marshal.PtrToStructure<Guid>(entry));
            }
            return ids;
        }
        finally
        {
            WlanFreeMemory(list);
        }
    }


    static List<Profile> GetProfiles(nint handle, Guid interfaceId)
    {
        var result = WlanGetProfileList(handle, in interfaceId, IntPtr.Zero, out var list);
        if (result != SUCCESS)
            return [];

        try
        {
            var count = Marshal.ReadInt32(list);
            var profiles = new List<Profile>(count);

            for (var i = 0; i < count; i++)
            {
                var entry = list + LIST_HEADER_SIZE + (i * PROFILE_INFO_SIZE);
                var name = Marshal.PtrToStringUni(entry, NAME_LENGTH)?.TrimEnd('\0');

                if (!String.IsNullOrEmpty(name))
                    profiles.Add(ReadProfile(handle, interfaceId, name));
            }
            return profiles;
        }
        finally
        {
            WlanFreeMemory(list);
        }
    }


    /// <remarks>
    /// The only description of a profile Windows will hand back is its XML, and the plaintext key
    /// inside it is withheld unless the caller is elevated - which is fine, because nothing here
    /// wants the key. A profile whose XML cannot be read still counts as saved, so it comes back
    /// with an unknown scheme rather than being dropped.
    /// </remarks>
    static Profile ReadProfile(nint handle, Guid interfaceId, string name)
    {
        uint flags = 0;
        var result = WlanGetProfile(handle, in interfaceId, name, IntPtr.Zero, out var xml, ref flags, IntPtr.Zero);
        if (result != SUCCESS || xml == IntPtr.Zero)
            return new Profile(interfaceId, name, WifiSecurity.Unknown, false);

        try
        {
            var content = Marshal.PtrToStringUni(xml);
            var parsed = WlanProfileParser.Parse(content);
            return new Profile(interfaceId, name, parsed.Security, parsed.IsHidden);
        }
        finally
        {
            WlanFreeMemory(xml);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    struct ConnectionParameters
    {
        public uint Mode;
        public nint Profile;
        public nint Ssid;
        public nint BssidList;
        public uint BssType;
        public uint Flags;
    }


    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanOpenHandle(uint clientVersion, nint reserved, out uint negotiatedVersion, out nint clientHandle);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanCloseHandle(nint clientHandle, nint reserved);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanEnumInterfaces(nint clientHandle, nint reserved, out nint interfaceList);

    [LibraryImport("wlanapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint WlanGetProfileList(nint clientHandle, in Guid interfaceGuid, nint reserved, out nint profileList);

    [LibraryImport("wlanapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint WlanGetProfile(nint clientHandle, in Guid interfaceGuid, string profileName, nint reserved, out nint profileXml, ref uint flags, nint grantedAccess);

    [LibraryImport("wlanapi.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial uint WlanDeleteProfile(nint clientHandle, in Guid interfaceGuid, string profileName, nint reserved);

    [LibraryImport("wlanapi.dll")]
    private static partial uint WlanConnect(nint clientHandle, in Guid interfaceGuid, in ConnectionParameters parameters, nint reserved);

    [LibraryImport("wlanapi.dll")]
    private static partial void WlanFreeMemory(nint memory);
}
