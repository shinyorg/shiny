namespace Shiny.Wearables;


/// <summary>
/// How Shiny.Wearables maps onto each platform, so a companion app written in Swift or Kotlin can speak it.
/// <para>
/// <b>Wear OS (Data Layer).</b> Both apps share the application id and signing key. The companion app advertises the
/// <see cref="Capability"/> capability (<c>res/values/wear.xml</c>, or <c>CapabilityClient.addLocalCapability</c>);
/// Shiny advertises it for this app at startup.
/// <list type="bullet">
/// <item>Messages: <c>MessageClient.sendRequest</c> to <c>/shiny/message/{path}</c>; the reply is the RPC result. A
/// plain <c>sendMessage</c> to the same path is also received, and not replied to.</item>
/// <item>Context: a data item at <c>/shiny/context</c> with a <c>data</c> byte array. Each node writes its own.</item>
/// <item>Transfers: an urgent data item at <c>/shiny/transfer/{id}</c> with <c>id</c>, <c>path</c> and <c>data</c>.
/// The receiver deletes it once handled, which is how the sender learns it arrived.</item>
/// <item>Files: an urgent data item at <c>/shiny/file/{id}</c> with <c>id</c>, <c>path</c>, <c>name</c>, a
/// <c>metadata</c> data map of strings, and the file as the <c>file</c> asset. Deleted by the receiver the same way.</item>
/// </list>
/// </para>
/// <para>
/// <b>watchOS (WatchConnectivity).</b>
/// <list type="bullet">
/// <item>Messages: <c>sendMessage(["path": String, "data": Data], replyHandler:)</c>; the reply is
/// <c>["data": Data]</c>. A message arriving from the watch is answered the same way.</item>
/// <item>Context: <c>updateApplicationContext(["data": Data])</c>.</item>
/// <item>Transfers: <c>transferUserInfo(["id": String, "path": String, "data": Data])</c>.</item>
/// <item>Files: <c>transferFile(url, metadata: ["id": String, "path": String, "name": String, "metadata": [String: String]])</c>.</item>
/// </list>
/// </para>
/// </summary>
public static class WearableProtocol
{
    /// <summary>The Wear OS capability the companion app advertises.</summary>
    public const string Capability = "shiny_wearable";

    /// <summary>Every Shiny Data Layer path starts with this.</summary>
    public const string Prefix = "/shiny";
    /// <summary>A message's Data Layer path is this plus the app path.</summary>
    public const string MessagePrefix = Prefix + "/message/";
    /// <summary>The data item each node writes its context to.</summary>
    public const string ContextPath = Prefix + "/context";
    /// <summary>A transfer's data item path is this plus its id.</summary>
    public const string TransferPrefix = Prefix + "/transfer/";
    /// <summary>A file's data item path is this plus its id.</summary>
    public const string FilePrefix = Prefix + "/file/";

    /// <summary>The transfer id.</summary>
    public const string IdKey = "id";
    /// <summary>The app path.</summary>
    public const string PathKey = "path";
    /// <summary>The body.</summary>
    public const string DataKey = "data";
    /// <summary>A file's name.</summary>
    public const string NameKey = "name";
    /// <summary>A file's string metadata.</summary>
    public const string MetadataKey = "metadata";
    /// <summary>The asset holding a file's content (Wear OS).</summary>
    public const string FileAssetKey = "file";

    /// <summary>
    /// Validates a path an app passes and trims its slashes: <c>/workout/start/</c> → <c>workout/start</c>.
    /// </summary>
    /// <exception cref="ArgumentException">Empty, or carries whitespace, <c>?</c> or <c>#</c>.</exception>
    public static string NormalizePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var trimmed = path.Trim('/');
        if (trimmed.Length == 0)
            throw new ArgumentException("A path is required, such as \"sync\".", nameof(path));

        foreach (var c in trimmed)
        {
            if (Char.IsWhiteSpace(c) || c == '?' || c == '#')
                throw new ArgumentException($"'{path}' is not a valid wearable path: no whitespace, '?' or '#'.", nameof(path));
        }
        return trimmed;
    }

    /// <summary>The Wear OS message path for an app path.</summary>
    public static string ToMessagePath(string path) => MessagePrefix + NormalizePath(path);

    /// <summary>The app path of a Wear OS message path, or null when the path is not a Shiny message.</summary>
    public static string? FromMessagePath(string? wirePath)
        => wirePath != null && wirePath.StartsWith(MessagePrefix, StringComparison.Ordinal) && wirePath.Length > MessagePrefix.Length
            ? wirePath[MessagePrefix.Length..]
            : null;

    /// <summary>A new transfer id.</summary>
    public static string NewTransferId() => Guid.NewGuid().ToString("N");

    /// <summary>
    /// Where a received file goes: its own folder under <paramref name="root"/>, named for the transfer, so two files
    /// with one name never collide. The name is reduced to its last segment so a sender cannot write outside it.
    /// </summary>
    public static string GetInboxPath(string root, string transferId, string fileName)
    {
        var safeName = Path.GetFileName(fileName.Replace('\\', '/').Split('/')[^1]);
        if (safeName.Length == 0 || safeName is "." or "..")
            safeName = "file";

        var safeId = Path.GetFileName(transferId);
        if (safeId.Length == 0 || safeId is "." or "..")
            safeId = NewTransferId();

        return Path.Combine(root, "Shiny.Wearables", safeId, safeName);
    }
}
