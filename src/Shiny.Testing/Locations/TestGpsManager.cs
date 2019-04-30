using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestGpsManager : IGpsManager
    {
        public AccessState Status { get; set; }
        public bool IsListening { get; set; }


        public IGpsReading LastGpsReading { get; set; }
        public IObservable<IGpsReading> GetLastReading() => Observable.Return(this.LastGpsReading);


        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess(bool backgroundMode) => Task.FromResult(this.RequestAccessReply);


        public GpsRequest LastGpsRequest { get; private set; }
        public Task StartListener(GpsRequest request = null)
        {
            this.LastGpsRequest = request;
            return Task.CompletedTask;
        }
        public Task StopListener() => Task.CompletedTask;


        public Subject<IGpsReading> ReadingSubject { get; } = new Subject<IGpsReading>();
        public IObservable<IGpsReading> WhenReading() => this.ReadingSubject;
    }
}
