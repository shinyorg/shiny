namespace Shiny.ScreenRecorder.Portal;


/// <summary>
/// The names and magic numbers of the xdg-desktop-portal ScreenCast API.
/// </summary>
/// <remarks>
/// Specified at <c>https://flatpak.github.io/xdg-desktop-portal/docs/</c>. The numeric values are
/// part of the wire protocol and are stable across portal implementations - GNOME's, KDE's and
/// wlroots' all agree on them.
/// </remarks>
internal static class PortalConstants
{
    public const string Service = "org.freedesktop.portal.Desktop";
    public const string ObjectPath = "/org/freedesktop/portal/desktop";
    public const string ScreenCastInterface = "org.freedesktop.portal.ScreenCast";
    public const string RequestInterface = "org.freedesktop.portal.Request";

    /// <summary>Source types, as a bitmask for <c>SelectSources</c>.</summary>
    public const uint SourceMonitor = 1;
    public const uint SourceWindow = 2;

    /// <summary>Cursor modes for <c>SelectSources</c>.</summary>
    public const uint CursorHidden = 1;
    public const uint CursorEmbedded = 2;

    /// <summary>The <c>response</c> code in a Request.Response signal.</summary>
    public const uint ResponseSuccess = 0;
    public const uint ResponseCancelled = 1;
    public const uint ResponseFailed = 2;
}
