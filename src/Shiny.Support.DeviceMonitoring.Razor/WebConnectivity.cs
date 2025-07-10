using Shiny.Net;

namespace Shiny.Support.DeviceMonitoring;

public class WebConnectivity : IConnectivity
{
    public IObservable<IConnectivity> WhenChanged() => throw new NotImplementedException();

    public ConnectionTypes ConnectionTypes { get; }
    public NetworkAccess Access { get; }
}