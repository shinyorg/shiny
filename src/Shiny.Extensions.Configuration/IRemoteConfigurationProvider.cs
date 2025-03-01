using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Extensions.Configuration;

public interface IRemoteConfigurationProvider : IConfigurationProvider
{
    DateTimeOffset? LastLoaded { get; }
    Task LoadAsync(CancellationToken cancellationToken = default);
    
    TimeSpan? AutoRefreshTimeSpan { get; }
    void StartAutoRefresh(TimeSpan refreshInterval);
    void StopAutoRefresh();
}