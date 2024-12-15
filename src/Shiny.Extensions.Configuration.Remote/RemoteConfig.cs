namespace Shiny.Extensions.Configuration;

public record RemoteConfig(
    string Uri,
    IConfiguration CurrentConfiguration,
    bool WaitForLoadOnStartup = false,
    TimeSpan? authRefreshTimer = null,
    string ConfigurationFilePath = "remotesettings.json"
);