namespace Shiny.Extensions.Configuration;

public interface IRemoteConfigurationProvider : IConfigurationProvider
{
    DateTimeOffset? LastLoaded { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
}