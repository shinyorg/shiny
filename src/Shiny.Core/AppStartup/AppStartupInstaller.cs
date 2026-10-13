using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if !(ANDROID || IOS || TVOS || MACCATALYST || MACOS)
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;
#endif

namespace Shiny;

public sealed partial class AppStartupInstaller : IAppStartupInstaller
{
    readonly AppStartupOptions options;

    public AppStartupInstaller(AppStartupOptions options)
        => this.options = options;

    public Task<AppStartupState> GetState(CancellationToken cancellationToken = default)
        => this.GetStateCore(cancellationToken);

    public Task<AppStartupState> Register(CancellationToken cancellationToken = default)
        => this.RegisterCore(cancellationToken);

    public Task<AppStartupState> Unregister(CancellationToken cancellationToken = default)
        => this.UnregisterCore(cancellationToken);

    public Task<bool> OpenSettings() => this.OpenSettingsCore();


    // the entry assembly rather than IPlatform.ApplicationIdentifier - the desktop implementations
    // run on the bare net10.0 target, where there is no bundle or package to ask
    string GetIdentifier()
    {
        var identifier = this.options.Identifier;
        if (String.IsNullOrWhiteSpace(identifier))
            identifier = Assembly.GetEntryAssembly()?.GetName().Name;

        if (String.IsNullOrWhiteSpace(identifier))
            identifier = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? String.Empty);

        if (String.IsNullOrWhiteSpace(identifier))
            throw new InvalidOperationException("Could not determine a startup identifier - set AppStartupOptions.Identifier");

        // The identifier becomes a file name on Linux and a registry value name on Windows, so refuse
        // anything that could escape those containers rather than silently writing somewhere else.
        if (identifier.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new InvalidOperationException($"AppStartupOptions.Identifier '{identifier}' contains characters that aren't valid in a file name");

        return identifier;
    }


    /// <summary>
    /// Resolves what the OS should launch. Quoting is left to each platform because the escaping rules
    /// differ (freedesktop desktop entries vs. a Windows command line).
    /// </summary>
    (string Executable, IReadOnlyList<string> Arguments) ResolveCommand()
    {
        var args = new List<string>();
        var exe = this.options.ExecutablePath;

        if (String.IsNullOrWhiteSpace(exe))
        {
            exe = Environment.ProcessPath;
            if (String.IsNullOrWhiteSpace(exe))
                throw new InvalidOperationException("Could not resolve the current executable path - set AppStartupOptions.ExecutablePath");

            // A framework-dependent app started as `dotnet app.dll` reports the shared host as its process
            // path, so the managed entry point has to be handed back as the first argument - otherwise the
            // OS would just launch the dotnet CLI at login.
            if (String.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase))
            {
                var entryPoint = Environment.GetCommandLineArgs().FirstOrDefault();
                if (!String.IsNullOrWhiteSpace(entryPoint))
                    args.Add(entryPoint);
            }
        }

        args.AddRange(this.options.Arguments);
        return (exe, args);
    }


#if ANDROID || IOS || TVOS

    // Neither OS has a startup list an app can add itself to.
    public bool IsSupported => false;

    Task<AppStartupState> GetStateCore(CancellationToken cancellationToken)
        => Task.FromResult(AppStartupState.NotSupported);

    Task<AppStartupState> RegisterCore(CancellationToken cancellationToken)
        => Task.FromResult(AppStartupState.NotSupported);

    Task<AppStartupState> UnregisterCore(CancellationToken cancellationToken)
        => Task.FromResult(AppStartupState.NotSupported);

    Task<bool> OpenSettingsCore() => Task.FromResult(false);

#elif !(MACCATALYST || MACOS)

    // Windows (both the windows target and plain .NET on Windows) and Linux (plain .NET - there is no
    // linux target framework). macOS, Catalyst and AppKit alike, is SMAppService in AppStartupInstaller.Apple.cs.

    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string StartupApprovedKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    const int AppModelErrorNoPackage = 15700;

    static readonly char[] QuoteRequiredChars = [' ', '\t', '"'];
    static bool? isPackagedApp;

    /// <summary>
    /// MSIX-packaged apps are excluded: the OS virtualizes their HKCU writes into a per-package hive, so a
    /// Run entry written by one is invisible to the shell. Those apps need a windows.startupTask manifest
    /// declaration driven through WinRT, which is not supported here.
    /// </summary>
    public bool IsSupported
        => this.IsLinuxSupported() || (OperatingSystem.IsWindows() && !IsPackagedApp());

    static bool IsLinux => OperatingSystem.IsLinux() && !OperatingSystem.IsAndroid();


    Task<AppStartupState> GetStateCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(AppStartupState.NotSupported);

        return OperatingSystem.IsWindows()
            ? Task.FromResult(this.GetWindowsState())
            : this.GetLinuxState(cancellationToken);
    }


    Task<AppStartupState> RegisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(AppStartupState.NotSupported);

        if (!OperatingSystem.IsWindows())
            return this.RegisterLinux(cancellationToken);

        this.WriteWindowsEntry();

        // Read back rather than assuming Enabled - the user may have switched the entry off previously and
        // Windows keeps that decision in a separate key that an app isn't allowed to overwrite.
        return this.GetStateCore(cancellationToken);
    }


    Task<AppStartupState> UnregisterCore(CancellationToken cancellationToken)
    {
        if (!this.IsSupported)
            return Task.FromResult(AppStartupState.NotSupported);

        if (!OperatingSystem.IsWindows())
            return this.UnregisterLinux(cancellationToken);

        this.DeleteWindowsEntry();
        return Task.FromResult(AppStartupState.NotRegistered);
    }


    Task<bool> OpenSettingsCore()
    {
        // Linux has no desktop-environment-agnostic settings page for autostart entries.
        if (!OperatingSystem.IsWindows())
            return Task.FromResult(false);

        try
        {
            // UseShellExecute is what lets the ms-settings: protocol handler take over.
            using var process = Process.Start(new ProcessStartInfo("ms-settings:startupapps") { UseShellExecute = true });
            return Task.FromResult(true);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
    }


    #region Windows

    [SupportedOSPlatform("windows")]
    AppStartupState GetWindowsState()
    {
        var identifier = this.GetIdentifier();

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        if (runKey?.GetValue(identifier) is not string value || String.IsNullOrWhiteSpace(value))
            return AppStartupState.NotRegistered;

        // Task Manager and Settings switch an entry off by flipping a status byte here instead of removing
        // the Run value - the low bit of the first byte is the on/off flag (0x02/0x06 on, 0x03/0x09 off).
        using var approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedKeyPath, false);
        if (approvedKey?.GetValue(identifier) is byte[] { Length: > 0 } flags && (flags[0] & 0x01) == 0x01)
            return AppStartupState.DisabledByUser;

        return AppStartupState.Enabled;
    }


    [SupportedOSPlatform("windows")]
    void WriteWindowsEntry()
    {
        var (exe, args) = this.ResolveCommand();
        var commandLine = String.Join(" ", args.Prepend(exe).Select(CommandLineQuote));

        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
        if (key == null)
            throw new InvalidOperationException($@"Could not open HKCU\{RunKeyPath} for writing");

        key.SetValue(this.GetIdentifier(), commandLine, RegistryValueKind.String);
    }


    [SupportedOSPlatform("windows")]
    void DeleteWindowsEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(this.GetIdentifier(), false);
    }


    [SupportedOSPlatform("windows")]
    static bool IsPackagedApp()
    {
        // Asking for the package name with a zero-length buffer is the documented way to tell whether the
        // process has package identity at all - unpackaged processes answer APPMODEL_ERROR_NO_PACKAGE.
        if (isPackagedApp == null)
        {
            var length = 0;
            isPackagedApp = GetCurrentPackageFullName(ref length, IntPtr.Zero) != AppModelErrorNoPackage;
        }
        return isPackagedApp.Value;
    }


    [DllImport("kernel32.dll", ExactSpelling = true)]
    static extern int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);


    // CommandLineToArgvW rules: backslashes are only special immediately before a quote, and a run of
    // trailing backslashes would otherwise escape the closing quote.
    internal static string CommandLineQuote(string value)
    {
        if (value.Length > 0 && value.IndexOfAny(QuoteRequiredChars) < 0)
            return value;

        var sb = new StringBuilder(value.Length + 2).Append('"');
        for (var i = 0; i < value.Length; i++)
        {
            var backslashes = 0;
            while (i < value.Length && value[i] == '\\')
            {
                backslashes++;
                i++;
            }

            if (i == value.Length)
            {
                sb.Append('\\', backslashes * 2);
                break;
            }

            if (value[i] == '"')
                sb.Append('\\', (backslashes * 2) + 1).Append('"');
            else
                sb.Append('\\', backslashes).Append(value[i]);
        }
        return sb.Append('"').ToString();
    }

    #endregion

    #region Linux

    bool IsSystemdMode => this.options.LinuxMode != LinuxStartupMode.XdgAutostart;
    bool IsSystemMode => this.options.LinuxMode == LinuxStartupMode.SystemdSystem;


    bool IsLinuxSupported()
    {
        if (!IsLinux)
            return false;

        return !this.IsSystemdMode || this.IsSystemdAvailable();
    }


    Task<AppStartupState> GetLinuxState(CancellationToken cancellationToken)
        => this.IsSystemdMode
            ? this.GetSystemdState(cancellationToken)
            : Task.FromResult(this.GetXdgState());


    async Task<AppStartupState> RegisterLinux(CancellationToken cancellationToken)
    {
        if (!this.IsSystemdMode)
        {
            this.WriteXdgEntry();
            return this.GetXdgState();
        }
        return await this.RegisterSystemd(cancellationToken).ConfigureAwait(false);
    }


    async Task<AppStartupState> UnregisterLinux(CancellationToken cancellationToken)
    {
        if (!this.IsSystemdMode)
        {
            this.DeleteXdgEntry();
            return AppStartupState.NotRegistered;
        }
        return await this.UnregisterSystemd(cancellationToken).ConfigureAwait(false);
    }


    AppStartupState GetXdgState()
    {
        var path = this.GetAutoStartPath();
        if (!File.Exists(path))
            return AppStartupState.NotRegistered;

        // Desktop environments switch an autostart entry off by flipping one of these keys rather than
        // deleting the file, so a file that exists isn't necessarily an entry that runs.
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Replace(" ", String.Empty).Trim();
            if (trimmed.Equals("Hidden=true", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("X-GNOME-Autostart-enabled=false", StringComparison.OrdinalIgnoreCase))
                return AppStartupState.DisabledByUser;
        }
        return AppStartupState.Enabled;
    }


    void WriteXdgEntry()
    {
        var (exe, args) = this.ResolveCommand();
        var path = this.GetAutoStartPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var sb = new StringBuilder();
        sb.AppendLine("[Desktop Entry]");
        sb.AppendLine("Type=Application");
        sb.AppendLine("Version=1.0");
        sb.Append("Name=").AppendLine(DesktopEntryValue(this.options.DisplayName ?? this.GetIdentifier()));
        sb.Append("Exec=").AppendLine(String.Join(" ", args.Prepend(exe).Select(DesktopEntryQuote)));
        sb.AppendLine("Terminal=false");
        sb.AppendLine("NoDisplay=false");
        // Both keys are written so an entry the user previously switched off comes back enabled.
        sb.AppendLine("Hidden=false");
        sb.AppendLine("X-GNOME-Autostart-enabled=true");

        File.WriteAllText(path, sb.ToString());
    }


    void DeleteXdgEntry()
    {
        var path = this.GetAutoStartPath();
        if (File.Exists(path))
            File.Delete(path);
    }


    string GetAutoStartPath()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (String.IsNullOrWhiteSpace(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return Path.Combine(configHome, "autostart", this.GetIdentifier() + ".desktop");
    }


    // Exec values are always quoted so paths with spaces work; inside quotes the desktop entry spec
    // requires backslash escaping of ", `, $ and \ itself.
    internal static string DesktopEntryQuote(string value)
    {
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("`", "\\`")
            .Replace("$", "\\$");

        return "\"" + escaped + "\"";
    }


    // A newline would start a new key in the entry file, so it never gets written through.
    internal static string DesktopEntryValue(string value)
        => value.Replace("\r", String.Empty).Replace("\n", " ");


    #region systemd

    const string ServiceSuffix = ".service";
    const string SystemUnitDirectory = "/etc/systemd/system";

    string UnitName => this.GetIdentifier() + ServiceSuffix;


    bool IsSystemdAvailable()
    {
        // The documented sd_booted() check - this directory only exists when systemd is PID 1.
        if (!Directory.Exists("/run/systemd/system"))
            return false;

        // A sandboxed app can't write to the host's unit directories or reach its service manager. Flatpak
        // apps should go through the Background portal instead.
        if (File.Exists("/.flatpak-info") || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAP")))
            return false;

        if (this.IsSystemMode)
            return true;

        // `systemctl --user` needs the per-user manager, which pam_systemd starts and exposes under the
        // user's runtime directory.
        var runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        return !String.IsNullOrWhiteSpace(runtimeDir) && Directory.Exists(Path.Combine(runtimeDir, "systemd"));
    }


    async Task<AppStartupState> GetSystemdState(CancellationToken cancellationToken)
    {
        var path = this.GetUnitPath();
        if (IsMasked(path))
            return AppStartupState.DisabledByPolicy;

        if (!File.Exists(path))
            return AppStartupState.NotRegistered;

        var (_, output, _) = await this.RunSystemctl(cancellationToken, "is-enabled", this.UnitName).ConfigureAwait(false);
        var state = output.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return state switch
        {
            "enabled" or "enabled-runtime" or "linked" or "linked-runtime" or "alias" => AppStartupState.Enabled,
            "masked" or "masked-runtime" => AppStartupState.DisabledByPolicy,
            // Register always enables, so a unit file that's present but disabled was switched off by hand.
            "disabled" => AppStartupState.DisabledByUser,
            _ => AppStartupState.NotRegistered
        };
    }


    async Task<AppStartupState> RegisterSystemd(CancellationToken cancellationToken)
    {
        this.AssertCanManageUnits();

        var path = this.GetUnitPath();

        // `systemctl mask` replaces the unit file with a /dev/null symlink - writing through it would do
        // nothing, and an app has no business unmasking what an administrator masked.
        if (IsMasked(path))
            return AppStartupState.DisabledByPolicy;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, this.BuildUnitFile());

        await this.RunSystemctlOrThrow(cancellationToken, "daemon-reload").ConfigureAwait(false);

        // Deliberately not `--now`: for a GUI app the process doing the registering is the one that would
        // be started, and a second instance is never what anyone wants. The unit starts on the next
        // login/boot, or immediately via `systemctl [--user] start`.
        await this.RunSystemctlOrThrow(cancellationToken, "enable", this.UnitName).ConfigureAwait(false);

        return await this.GetSystemdState(cancellationToken).ConfigureAwait(false);
    }


    async Task<AppStartupState> UnregisterSystemd(CancellationToken cancellationToken)
    {
        var path = this.GetUnitPath();
        if (IsMasked(path))
            return AppStartupState.DisabledByPolicy;

        if (!File.Exists(path))
            return AppStartupState.NotRegistered;

        this.AssertCanManageUnits();

        // Disable first so the WantedBy symlink goes away with the unit - a dangling one makes systemd
        // log a warning on every boot. Failure here isn't fatal; the unit file is removed either way.
        await this.RunSystemctl(cancellationToken, "disable", this.UnitName).ConfigureAwait(false);
        File.Delete(path);
        await this.RunSystemctl(cancellationToken, "daemon-reload").ConfigureAwait(false);

        return AppStartupState.NotRegistered;
    }


    void AssertCanManageUnits()
    {
        if (this.IsSystemMode && !Environment.IsPrivilegedProcess)
            throw new InvalidOperationException($"LinuxStartupMode.SystemdSystem writes to {SystemUnitDirectory} and requires root - run the installer with sudo or use LinuxStartupMode.SystemdUser");
    }


    string GetUnitPath()
    {
        var identifier = this.GetIdentifier();

        // Unit names are a restricted alphabet - '@' would turn the unit into a template, so it's refused too.
        foreach (var c in identifier)
        {
            if (!(Char.IsAsciiLetterOrDigit(c) || c is ':' or '_' or '.' or '-'))
                throw new InvalidOperationException($"AppStartupOptions.Identifier '{identifier}' is not a valid systemd unit name - use letters, digits and ':', '_', '.', '-' only");
        }

        if (this.IsSystemMode)
            return Path.Combine(SystemUnitDirectory, identifier + ServiceSuffix);

        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (String.IsNullOrWhiteSpace(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");

        return Path.Combine(configHome, "systemd", "user", identifier + ServiceSuffix);
    }


    static bool IsMasked(string path)
    {
        var info = new FileInfo(path);
        return info.LinkTarget == "/dev/null";
    }


    string BuildUnitFile()
    {
        var (exe, args) = this.ResolveCommand();

        // ExecStart's first word has to be absolute - systemd won't search PATH in older releases, and a
        // relative path would resolve against whatever directory the manager happens to be in.
        if (!Path.IsPathRooted(exe))
            throw new InvalidOperationException($"AppStartupOptions.ExecutablePath '{exe}' must be an absolute path for a systemd unit");

        var workingDir = this.options.WorkingDirectory;
        if (String.IsNullOrWhiteSpace(workingDir))
            workingDir = AppContext.BaseDirectory;

        var mode = this.options.LinuxMode;
        var sb = new StringBuilder();
        sb.AppendLine("[Unit]");
        sb.Append("Description=").AppendLine(SystemdValue(this.options.DisplayName ?? this.GetIdentifier()));
        switch (mode)
        {
            case LinuxStartupMode.SystemdGraphicalSession:
                // PartOf stops the app when the session ends; After waits for the display to be available.
                sb.AppendLine("PartOf=graphical-session.target");
                sb.AppendLine("After=graphical-session.target");
                break;

            case LinuxStartupMode.SystemdSystem:
                sb.AppendLine("After=network.target");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("[Service]");
        sb.AppendLine("Type=simple");
        sb.Append("ExecStart=").AppendLine(String.Join(" ", args.Prepend(exe).Select(SystemdQuote)));
        sb.Append("WorkingDirectory=").AppendLine(SystemdValue(workingDir.TrimEnd('/')));

        // A framework-dependent app with a private runtime install (~/.dotnet) only finds it through
        // DOTNET_ROOT, which systemd won't inherit from the user's shell.
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!String.IsNullOrWhiteSpace(dotnetRoot))
            sb.Append("Environment=").AppendLine(SystemdQuote("DOTNET_ROOT=" + dotnetRoot));

        if (mode == LinuxStartupMode.SystemdSystem && !String.IsNullOrWhiteSpace(this.options.LinuxServiceUser))
        {
            var user = this.options.LinuxServiceUser;
            if (user.Any(Char.IsWhiteSpace))
                throw new InvalidOperationException($"AppStartupOptions.LinuxServiceUser '{user}' is not a valid account name");

            sb.Append("User=").AppendLine(SystemdValue(user));
        }

        if (this.options.RestartOnFailure)
        {
            sb.AppendLine("Restart=on-failure");
            sb.AppendLine("RestartSec=5");
        }

        sb.AppendLine();
        sb.AppendLine("[Install]");
        sb.Append("WantedBy=").AppendLine(mode switch
        {
            LinuxStartupMode.SystemdGraphicalSession => "graphical-session.target",
            LinuxStartupMode.SystemdSystem => "multi-user.target",
            _ => "default.target"
        });

        return sb.ToString();
    }


    Task<(int ExitCode, string Output, string Error)> RunSystemctl(CancellationToken cancellationToken, params string[] args)
        => RunSystemctl(!this.IsSystemMode, cancellationToken, args);


    async Task RunSystemctlOrThrow(CancellationToken cancellationToken, params string[] args)
    {
        var (exitCode, _, error) = await this.RunSystemctl(cancellationToken, args).ConfigureAwait(false);
        if (exitCode != 0)
            throw new InvalidOperationException($"systemctl {String.Join(" ", args)} failed ({exitCode}): {error.Trim()}");
    }


    static async Task<(int ExitCode, string Output, string Error)> RunSystemctl(bool user, CancellationToken cancellationToken, string[] args)
    {
        var psi = new ProcessStartInfo("systemctl")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        if (user)
            psi.ArgumentList.Add("--user");

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Could not start systemctl");

        // Both streams are drained concurrently - reading one to the end first can deadlock once the other
        // fills its pipe buffer.
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return (process.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
    }


    // systemd command-line quoting: double quotes with C-style backslash escapes. '%' starts a unit
    // specifier and '$' an environment expansion, so both are doubled to stay literal.
    internal static string SystemdQuote(string value)
    {
        var escaped = SystemdValue(value)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "$$");

        return "\"" + escaped + "\"";
    }


    // Newlines would start a new key in the unit file, and '%' is expanded as a specifier in most settings.
    internal static string SystemdValue(string value)
        => value.Replace("\r", String.Empty).Replace("\n", " ").Replace("%", "%%");

    #endregion


    #endregion
#endif
}
