using System.Globalization;

namespace Shiny.AppStore;

/// <summary>
/// Linux implementation of <see cref="IAppStore"/> over the two packaging formats that publish an
/// updatable version to a store: Flatpak and Snap. An app installed any other way (a tarball, a distro
/// package, <c>dotnet run</c>) has no store to ask, so <see cref="GetCurrent"/> returns null there.
/// </summary>
public sealed class LinuxAppStore : IAppStore
{
    readonly AppStoreOptions options;
    readonly IPlatform platform;

    // How the app is packaged can't change while it runs, and working it out costs two process
    // launches when there's no sandbox to read it off, so it's resolved once.
    LinuxPackageFormat? format;

    public LinuxAppStore(IPlatform platform, AppStoreOptions options)
    {
        this.platform = platform;
        this.options = options;
    }


    public async Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default)
    {
        var appId = this.ResolveAppId();
        if (String.IsNullOrWhiteSpace(appId))
            return null;

        var packageFormat = this.format ??= await this.ResolveFormat(appId, cancellationToken).ConfigureAwait(false);
        return packageFormat switch
        {
            LinuxPackageFormat.Flatpak => await this.GetFlatpak(appId, cancellationToken).ConfigureAwait(false),
            LinuxPackageFormat.Snap => await this.GetSnap(appId, cancellationToken).ConfigureAwait(false),
            _ => null
        };
    }


    /// <summary>
    /// Hands the AppStream component ID to the desktop's software centre (GNOME Software, Plasma
    /// Discover, Snap Store). False when no ID is configured or nothing is registered for the scheme.
    /// </summary>
    public Task<bool> OpenStore()
    {
        var appId = this.ResolveAppId();
        if (String.IsNullOrWhiteSpace(appId))
            return Task.FromResult(false);

        return LinuxCommand.Open("appstream://" + appId);
    }


    /// <summary>
    /// Software centres show reviews on the app's own page, so this is the same destination as
    /// <see cref="OpenStore"/>.
    /// </summary>
    public Task<bool> OpenReviewPage() => this.OpenStore();


    /// <summary>
    /// No Linux software centre offers an in-app review prompt, so this falls back to opening the
    /// app's page in the software centre.
    /// </summary>
    public Task<bool> RequestReview() => this.OpenReviewPage();


    string? ResolveAppId()
    {
        if (!String.IsNullOrWhiteSpace(this.options.LinuxAppId))
            return this.options.LinuxAppId;

        // the Flatpak app ID or Snap name, set by the respective runtime inside the running app
        var id = this.platform.ApplicationIdentifier;
        if (!String.IsNullOrWhiteSpace(id))
            return id;

        // Last resort - the entry assembly name, which only matches when the app was published under that name.
        id = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
        return String.IsNullOrWhiteSpace(id) ? null : id;
    }


    async Task<LinuxPackageFormat> ResolveFormat(string appId, CancellationToken cancellationToken)
    {
        if (LinuxCommand.IsFlatpakSandbox || !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FLATPAK_ID")))
            return LinuxPackageFormat.Flatpak;

        if (!String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SNAP_NAME")))
            return LinuxPackageFormat.Snap;

        // Not running from a sandbox. The ID may still have been configured by hand (a developer build
        // checking the published version), so ask each tool whether it knows the app.
        if (await LinuxCommand.ReadFlatpak(["info", appId], cancellationToken).ConfigureAwait(false) != null)
            return LinuxPackageFormat.Flatpak;

        if (await LinuxCommand.Read("snap", ["list", appId], cancellationToken).ConfigureAwait(false) != null)
            return LinuxPackageFormat.Snap;

        return LinuxPackageFormat.None;
    }


    #region Flatpak

    async Task<AppStoreResult?> GetFlatpak(string appId, CancellationToken cancellationToken)
    {
        var installed = ParseFields(await LinuxCommand.ReadFlatpak(["info", appId], cancellationToken).ConfigureAwait(false));

        // The origin is the remote the app was installed from - the only remote whose published version
        // is meaningful for this install.
        if (!installed.TryGetValue("Origin", out var remote) || String.IsNullOrWhiteSpace(remote))
            return null;

        var remoteInfo = ParseFields(await LinuxCommand.ReadFlatpak(["remote-info", remote, appId], cancellationToken).ConfigureAwait(false));
        if (!TryGetVersion(remoteInfo, "Version", out var storeVersion))
            return null;

        var current = TryGetVersion(installed, "Version", out var installedVersion)
            ? installedVersion
            : this.platform.AppVersion;

        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            // Flathub is the only remote with a predictable web address; a private remote has none.
            remote.Equals("flathub", StringComparison.OrdinalIgnoreCase) ? $"https://flathub.org/apps/{appId}" : String.Empty,
            // The commit subject is the closest thing a Flatpak remote publishes to release notes.
            remoteInfo.GetValueOrDefault("Subject"),
            TryGetDate(remoteInfo, "Date")
        );
    }


    /// <summary>
    /// Turns the <c>Key: value</c> block both <c>flatpak info</c> and <c>flatpak remote-info</c> print
    /// into a lookup. Keys are right-aligned with leading spaces, hence the trim on both halves.
    /// </summary>
    internal static Dictionary<string, string> ParseFields(string? output)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (String.IsNullOrWhiteSpace(output))
            return fields;

        foreach (var line in output.Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // The header line ("Name - summary") and the ref/date lines that contain further colons are
            // fine here; only the first colon splits, and a duplicate key keeps the first occurrence.
            if (key.Length > 0 && value.Length > 0)
                fields.TryAdd(key, value);
        }
        return fields;
    }


    static DateTimeOffset? TryGetDate(Dictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var value) && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;

    #endregion

    #region Snap

    async Task<AppStoreResult?> GetSnap(string appId, CancellationToken cancellationToken)
    {
        var list = await LinuxCommand.Read("snap", ["list", appId], cancellationToken).ConfigureAwait(false);
        if (!TryParseSnapList(list, out var installedVersion, out var tracking))
            return null;

        var info = await LinuxCommand.Read("snap", ["info", appId], cancellationToken).ConfigureAwait(false);
        if (info == null)
            return null;

        if (!TryParseSnapChannel(info, tracking, out var storeVersionText, out var releasedAt))
            return null;

        if (!TryParseVersion(storeVersionText, out var storeVersion))
            return null;

        var current = TryParseVersion(installedVersion, out var parsedInstalled) ? parsedInstalled : this.platform.AppVersion;

        return new AppStoreResult(
            storeVersion,
            current,
            storeVersion > current,
            ParseSnapStoreUrl(info) ?? $"https://snapcraft.io/{appId}",
            null,
            releasedAt
        );
    }


    /// <summary>
    /// <c>snap list &lt;name&gt;</c> prints a header row then one whitespace-separated row:
    /// <c>Name Version Rev Tracking Publisher Notes</c>.
    /// </summary>
    internal static bool TryParseSnapList(string? output, out string version, out string tracking)
    {
        version = String.Empty;
        tracking = String.Empty;
        if (String.IsNullOrWhiteSpace(output))
            return false;

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
            return false;

        var columns = lines[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (columns.Length < 4)
            return false;

        version = columns[1];
        tracking = columns[3];
        return true;
    }


    /// <summary>
    /// Reads the version published on the channel the install tracks out of the <c>channels:</c> block
    /// of <c>snap info</c>, e.g. <c>  latest/stable: 1.3.0 2026-02-01 (55) 60MB -</c>. A channel that
    /// just closes over the one above it is written as <c>^</c>, and one with no release as <c>--</c>.
    /// </summary>
    internal static bool TryParseSnapChannel(string info, string tracking, out string version, out DateTimeOffset? releasedAt)
    {
        version = String.Empty;
        releasedAt = null;

        var inChannels = false;
        var lastConcrete = (Version: String.Empty, ReleasedAt: (DateTimeOffset?)null);

        foreach (var line in info.Split('\n'))
        {
            if (!inChannels)
            {
                inChannels = line.StartsWith("channels:", StringComparison.Ordinal);
                continue;
            }

            // The block ends at the next unindented key (usually "installed:").
            if (line.Length > 0 && !Char.IsWhiteSpace(line[0]))
                break;

            var separator = line.IndexOf(':');
            if (separator <= 0)
                continue;

            var channel = line[..separator].Trim();
            var columns = line[(separator + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length == 0)
                continue;

            if (columns[0] == "--")
                continue;

            if (columns[0] != "^")
            {
                lastConcrete = (
                    columns[0],
                    columns.Length > 1 && DateTimeOffset.TryParse(columns[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                        ? parsed
                        : null
                );
            }

            if (!channel.Equals(tracking, StringComparison.OrdinalIgnoreCase))
                continue;

            if (String.IsNullOrEmpty(lastConcrete.Version))
                return false;

            version = lastConcrete.Version;
            releasedAt = lastConcrete.ReleasedAt;
            return true;
        }
        return false;
    }


    internal static string? ParseSnapStoreUrl(string info)
    {
        foreach (var line in info.Split('\n'))
        {
            if (line.StartsWith("store-url:", StringComparison.Ordinal))
                return line["store-url:".Length..].Trim();
        }
        return null;
    }

    #endregion

    static bool TryGetVersion(Dictionary<string, string> fields, string key, out Version version)
    {
        version = new Version(0, 0);
        return fields.TryGetValue(key, out var value) && TryParseVersion(value, out version);
    }


    /// <summary>
    /// Flatpak and Snap versions are free-form strings ("v1.2.3", "1.2.3-beta.1", "2026.1"), so the
    /// leading numeric run is taken and the rest discarded - enough to answer "is the store ahead".
    /// </summary>
    internal static bool TryParseVersion(string? value, out Version version)
    {
        version = new Version(0, 0);
        if (String.IsNullOrWhiteSpace(value))
            return false;

        var span = value.AsSpan().Trim();
        if (span.Length > 0 && (span[0] == 'v' || span[0] == 'V'))
            span = span[1..];

        var length = 0;
        while (length < span.Length && (Char.IsAsciiDigit(span[length]) || span[length] == '.'))
            length++;

        var trimmed = span[..length].TrimEnd('.');

        // Version.TryParse rejects a bare major ("2026"), which is a perfectly normal snap version.
        if (trimmed.IndexOf('.') < 0)
        {
            if (!Int32.TryParse(trimmed, out var major))
                return false;

            version = new Version(major, 0);
            return true;
        }
        return Version.TryParse(trimmed, out version!);
    }
}


enum LinuxPackageFormat
{
    None,
    Flatpak,
    Snap
}
