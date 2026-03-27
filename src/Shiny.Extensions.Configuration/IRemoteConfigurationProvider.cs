namespace Shiny.Extensions.Configuration;

/// <summary>
/// Provides remote configuration loading with async support and load tracking
/// </summary>
public interface IRemoteConfigurationProvider : IConfigurationProvider
{
    /// <summary>
    /// Gets the timestamp of the last successful configuration load
    /// </summary>
    DateTimeOffset? LastLoaded { get; }

    /// <summary>
    /// Asynchronously loads configuration from the remote source
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LoadAsync(CancellationToken cancellationToken = default);
}