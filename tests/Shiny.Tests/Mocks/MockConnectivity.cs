using System.Reactive.Subjects;
using Shiny.Net;

namespace Shiny.Tests.Mocks;

public class MockConnectivity : Shiny.Net.IConnectivity
{
    public void Change(NetworkAccess netAccess = NetworkAccess.Internet, ConnectionTypes connTypes = ConnectionTypes.Wifi)
    {
        this.Access = netAccess;
        this.ConnectionTypes = connTypes;
        this.changeSubj.OnNext(this);
    }


    readonly Subject<IConnectivity> changeSubj = new();
    public ConnectionTypes ConnectionTypes { get; private set; } = ConnectionTypes.Wifi;
    public NetworkAccess Access { get; private set; } = NetworkAccess.Internet;
    public IObservable<IConnectivity> WhenChanged() => this.changeSubj.StartWith(this);
}

