namespace Sample.Blazor.Layout;

/// <summary>
/// Whether the sample is running inside a shinylib.net documentation page. The docs load a demo in
/// an iframe with <c>?embed=1</c> (and <c>&amp;theme=dark|light</c>); <see cref="EmbedLayout"/> then
/// replaces <see cref="MainLayout"/> so the page loses the sample's own navigation and
/// chrome. Read once from the first URL and kept for the life of the app, so following a link
/// inside an embedded demo stays embedded.
/// </summary>
public static class EmbedMode
{
    static bool detected;

    public static bool IsActive { get; private set; }

    /// <summary>The scheme the docs page asked for when it loaded the frame (dark or light), or null to keep the default.</summary>
    public static string? InitialTheme { get; private set; }

    public static void Detect(string uri)
    {
        if (detected)
            return;

        detected = true;

        // a repeated key keeps its last value - the docs never repeat one, and ToDictionary would throw on it
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        var parts = new Uri(uri).Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2));

        foreach (var part in parts)
            pairs[Uri.UnescapeDataString(part[0]).ToLowerInvariant()] = part.Length > 1 ? Uri.UnescapeDataString(part[1]).ToLowerInvariant() : "";

        IsActive = pairs.TryGetValue("embed", out var embed) && embed is "" or "1" or "true";

        // only the two schemes the docs site has; anything else leaves the gallery's default alone
        if (pairs.TryGetValue("theme", out var theme) && theme is "dark" or "light")
            InitialTheme = theme;
    }
}
