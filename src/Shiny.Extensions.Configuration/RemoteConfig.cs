namespace Shiny.Extensions.Configuration;

public record RemoteConfig(
    string Uri,
    IConfiguration CurrentConfiguration,
    bool WaitForLoadOnStartup = false,
    string ConfigurationFilePath = "remotesettings.json"
);