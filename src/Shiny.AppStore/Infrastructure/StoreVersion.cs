namespace Shiny.AppStore.Infrastructure;


static class StoreVersion
{
    /// <summary>
    /// Parses a store version the way <see cref="IPlatform.AppVersion"/> reads the running app's: a single-part version
    /// ("2") is padded to "2.0", which <see cref="Version.TryParse(string?, out Version?)"/> would otherwise reject.
    /// </summary>
    public static bool TryParse(string? value, out Version version)
    {
        version = new Version(0, 0);
        var trimmed = value?.Trim();
        if (String.IsNullOrEmpty(trimmed))
            return false;

        if (!trimmed.Contains('.'))
            trimmed += ".0";

        if (!Version.TryParse(trimmed, out var parsed))
            return false;

        version = parsed;
        return true;
    }
}
