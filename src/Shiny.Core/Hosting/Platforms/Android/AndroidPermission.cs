namespace Shiny;

public record AndroidPermission(
    string Permission,
    int? MinSdkVersion,
    int? MaxSdkVersion
);