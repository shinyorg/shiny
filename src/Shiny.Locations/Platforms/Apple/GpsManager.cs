using System;
using System.Threading.Tasks;

namespace Shiny.Locations;


public class GpsManager : IGpsManager, IShinyStartupTask
{
    public GpsRequest? CurrentListener { get; }
    public AccessState GetCurrentStatus(GpsRequest request) => throw new NotImplementedException();

    public Task<AccessState> RequestAccess(GpsRequest request) => throw new NotImplementedException();

    public IObservable<GpsReading?> GetLastReading() => throw new NotImplementedException();

    public IObservable<GpsReading> WhenReading() => throw new NotImplementedException();

    public Task StartListener(GpsRequest request) => throw new NotImplementedException();

    public Task StopListener() => throw new NotImplementedException();

    public void Start() => throw new NotImplementedException();
}