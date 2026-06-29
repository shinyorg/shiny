using System;
using Shiny.Net;

namespace Shiny.Net.Http.Tests.Fakes;


/// <summary>Always-on Wi-Fi connectivity for the transfer-loop tests.</summary>
public sealed class FakeConnectivity : IConnectivity
{
    public event EventHandler? Changed;

    public ConnectionTypes ConnectionTypes { get; set; } = ConnectionTypes.Wifi;
    public NetworkAccess Access { get; set; } = NetworkAccess.Internet;

    public void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
}
