using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny;

/// <summary>
/// Installs or removes the current application from the operating system's "launch at login/startup"
/// list on desktop platforms (Windows, macOS, Linux). On Linux the app can also be installed as a systemd
/// unit - either tied to the graphical session (GUI apps) or as a headless user/system service (see
/// <see cref="AppStartupOptions.LinuxMode"/>). Mobile platforms have no equivalent concept and
/// report <see cref="AppStartupState.NotSupported"/> on every call.
/// </summary>
public interface IAppStartupInstaller
{
    /// <summary>
    /// True when the running platform can manage startup registration - Windows (unpackaged), macOS 13+,
    /// and Linux (the systemd modes additionally need a systemd-booted host with a reachable service
    /// manager, outside a Flatpak/Snap sandbox). Check this before showing a "run at startup" toggle; everything else returns
    /// <see cref="AppStartupState.NotSupported"/>.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Reads the current registration state back from the OS. The user (or an administrator) can turn a
    /// registered app off outside of your app, so never cache this across app sessions.
    /// </summary>
    Task<AppStartupState> GetState(CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs the app into the OS startup list and returns the state the OS settled on. The result can
    /// come back as <see cref="AppStartupState.DisabledByUser"/>, <see cref="AppStartupState.DisabledByPolicy"/>,
    /// or <see cref="AppStartupState.RequiresApproval"/> when the user or policy has the final say -
    /// that is not a failure, it means the user has to finish the job in the OS settings
    /// (see <see cref="OpenSettings"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The OS rejected the registration outright, or <see cref="LinuxStartupMode.SystemdSystem"/> was used
    /// by a process that isn't running as root.
    /// </exception>
    Task<AppStartupState> Register(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the app from the OS startup list. Succeeds silently when the app was never registered.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="LinuxStartupMode.SystemdSystem"/> only - the process isn't running as root.
    /// </exception>
    Task<AppStartupState> Unregister(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the OS screen where the user manages startup apps (macOS System Settings > General > Login Items,
    /// Windows Settings > Apps > Startup). Returns false where there is nothing to open (Linux, mobile).
    /// </summary>
    Task<bool> OpenSettings();
}


public enum AppStartupState
{
    /// <summary>The platform has no startup list this library can manage.</summary>
    NotSupported,

    /// <summary>The app is not in the startup list.</summary>
    NotRegistered,

    /// <summary>The app is registered and will launch at login.</summary>
    Enabled,

    /// <summary>Registered, but the user turned it off in Task Manager / Settings / Login Items.</summary>
    DisabledByUser,

    /// <summary>
    /// Registered, but group policy or MDM blocks it (or, on Linux, the systemd unit is masked). The app
    /// cannot override this.
    /// </summary>
    DisabledByPolicy,

    /// <summary>
    /// macOS only - the login item was submitted but the user has to approve it in System Settings
    /// before it will actually launch.
    /// </summary>
    RequiresApproval
}


public class AppStartupOptions
{
    /// <summary>
    /// Identity of the startup entry. Defaults to the entry assembly name.
    /// <list type="bullet">
    /// <item>Windows: the value name written under <c>HKCU\...\CurrentVersion\Run</c>. MSIX-packaged apps
    /// aren't supported - the OS virtualizes that key for them.</item>
    /// <item>Linux: the file name of the <c>~/.config/autostart/{Identifier}.desktop</c> entry, or the
    /// <c>{Identifier}.service</c> unit name in the systemd modes (letters, digits and <c>:_.-</c> only).</item>
    /// <item>macOS: ignored - <c>SMAppService</c> always registers the running app bundle.</item>
    /// </list>
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// Friendly name written into the Linux desktop entry's <c>Name</c> key, or the systemd unit's
    /// <c>Description</c>. Defaults to
    /// <see cref="Identifier"/>. Ignored on Windows and macOS, which take the name from the app itself.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Executable the OS should launch. Defaults to the current process path, expanding to
    /// <c>dotnet &lt;entry assembly&gt;</c> when the app runs framework-dependent through the shared host.
    /// Ignored on macOS, where the OS launches the registered app bundle itself.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Arguments appended to the startup command line - commonly a marker such as <c>--autostart</c> so the
    /// app can start minimized or in the tray. Honoured on Windows and Linux; macOS login items launch the
    /// app bundle itself and cannot carry arguments.
    /// </summary>
    public IList<string> Arguments { get; set; } = new List<string>();

    /// <summary>
    /// How the app is started on Linux. Defaults to <see cref="LinuxStartupMode.XdgAutostart"/>, the most
    /// portable option for a GUI app. Ignored on Windows and macOS.
    /// </summary>
    public LinuxStartupMode LinuxMode { get; set; } = LinuxStartupMode.XdgAutostart;

    /// <summary>
    /// systemd modes only - writes <c>Restart=on-failure</c> so systemd relaunches the process when it
    /// crashes (a clean exit is left alone). Defaults to true.
    /// </summary>
    public bool RestartOnFailure { get; set; } = true;

    /// <summary>
    /// systemd modes only - the unit's <c>WorkingDirectory</c>. Defaults to the app's base directory, so
    /// relative paths (and a Generic Host content root) resolve the same as when launched by hand.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// <see cref="LinuxStartupMode.SystemdSystem"/> only - the account the service runs as
    /// (<c>User=</c>). Defaults to root when not set.
    /// </summary>
    public string? LinuxServiceUser { get; set; }
}


/// <summary>
/// How <see cref="IAppStartupInstaller"/> starts the app on Linux.
/// </summary>
public enum LinuxStartupMode
{
    /// <summary>
    /// GUI app. A freedesktop autostart entry at <c>~/.config/autostart/{Identifier}.desktop</c>, launched
    /// by the desktop environment at login. Works on any XDG-compliant desktop, systemd or not.
    /// </summary>
    XdgAutostart,

    /// <summary>
    /// GUI app. A systemd user unit at <c>~/.config/systemd/user/{Identifier}.service</c> bound to
    /// <c>graphical-session.target</c> - started once the desktop session is up and stopped when it ends,
    /// with crash restarts and journald logging. The session has to reach <c>graphical-session.target</c>
    /// (GNOME and KDE Plasma do; bare window managers need it set up).
    /// </summary>
    SystemdGraphicalSession,

    /// <summary>
    /// Headless service. A systemd user unit at <c>~/.config/systemd/user/{Identifier}.service</c> wanted by
    /// <c>default.target</c> - runs when the user's service manager starts (first login, or at boot when
    /// lingering is enabled with <c>loginctl enable-linger</c>). No root required.
    /// </summary>
    SystemdUser,

    /// <summary>
    /// Headless service. A systemd system unit at <c>/etc/systemd/system/{Identifier}.service</c> wanted by
    /// <c>multi-user.target</c> - runs at boot with no login. Register/Unregister require root.
    /// </summary>
    SystemdSystem
}
