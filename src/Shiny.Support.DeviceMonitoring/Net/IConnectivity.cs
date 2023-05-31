using System;

namespace Shiny.Net;


public interface IConnectivity
{
    IObservable<IConnectivity> WhenChanged();
    ConnectionTypes ConnectionTypes { get; }
    NetworkAccess Access { get; }
}