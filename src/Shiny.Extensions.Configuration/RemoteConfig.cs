namespace Shiny.Extensions.Configuration;

/// <summary>
/// Describes a remote configuration source loaded over HTTP and cached to disk.
/// </summary>
/// <param name="Uri">The remote endpoint that returns the configuration payload.</param>
/// <param name="CurrentConfiguration">The configuration loaded so far (from the on-disk cache).</param>
/// <param name="WaitForLoadOnStartup">When true, the configuration build blocks until the remote load completes.</param>
/// <param name="ConfigurationFilePath">Local file path where the remote configuration is cached.</param>
public record RemoteConfig(
    string Uri,
    IConfiguration CurrentConfiguration,
    bool WaitForLoadOnStartup = false,
    string ConfigurationFilePath = "remotesettings.json"
);