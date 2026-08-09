using System.Net.Sockets;

namespace Shiny.Net.Discovery.Managed.Multicast;


/// <summary>
/// Turns the anonymous socket errors the OS raises when a multicast permission is missing into an
/// exception that names the thing to add.
/// </summary>
/// <remarks>
/// Every platform reports "you did not ask for permission" as either a generic socket error or,
/// worse, as silence. A consumer hitting that sees "no devices found" and has no way to tell it
/// apart from an empty network, so the guesses here are deliberately eager - a slightly wrong hint
/// is far more useful than none.
/// </remarks>
static class PermissionDiagnostics
{
    /// <summary>
    /// Maps a socket failure to a permission exception, or returns null when the error has nothing
    /// to do with permissions.
    /// </summary>
    public static DiscoveryPermissionException? Translate(SocketException ex, string protocol)
    {
        // Android LNP denial surfaces as EPERM/EACCES on the send; Apple's local network denial
        // and a missing multicast entitlement come back as unreachable
        var isDenial = ex.SocketErrorCode
            is SocketError.AccessDenied
            or SocketError.NetworkDown
            or SocketError.NetworkUnreachable
            or SocketError.HostUnreachable;

        if (!isDenial)
            return null;

        return new DiscoveryPermissionException(BuildMessage(protocol), ex);
    }


    static string BuildMessage(string protocol)
    {
        if (OperatingSystem.IsAndroid())
        {
            return $"The OS refused {protocol} multicast. Add CHANGE_WIFI_MULTICAST_STATE and INTERNET to AndroidManifest.xml, and - when targeting SDK 37 (Android 17) or later - request the android.permission.ACCESS_LOCAL_NETWORK runtime permission before discovering.";
        }

        if (OperatingSystem.IsIOS())
        {
            return $"The OS refused {protocol} multicast. Unlike mDNS, {protocol} needs the com.apple.developer.networking.multicast entitlement, which Apple grants per developer team on request (https://developer.apple.com/contact/request/networking-multicast). Set NSLocalNetworkUsageDescription in Info.plist as well. This cannot be tested in the simulator.";
        }

        if (OperatingSystem.IsMacCatalyst() || OperatingSystem.IsMacOS())
        {
            return $"The OS refused {protocol} multicast. A sandboxed app needs the com.apple.security.network.client entitlement (plus .network.server to advertise), and macOS 15+ additionally prompts for Local Network access - check System Settings > Privacy & Security > Local Network. The iOS multicast entitlement is NOT used here.";
        }

        if (OperatingSystem.IsWindows())
        {
            return $"The OS refused {protocol} multicast. A packaged (MSIX/WinUI) app needs the privateNetworkClientServer capability in Package.appxmanifest, and the firewall must allow inbound UDP on the port.";
        }

        return $"The OS refused {protocol} multicast. Check that the firewall allows the UDP port and that the interface supports multicast (containers on a bridge network usually do not - use host or macvlan networking).";
    }
}
