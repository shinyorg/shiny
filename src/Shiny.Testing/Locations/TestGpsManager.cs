using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestGpsManager : IGpsManager
    {
        readonly Subject<AccessState> accessSubject = new Subject<AccessState>();


        AccessState replyStatus = AccessState.Available;
        public AccessState ReplyStatus
        {
            get => this.replyStatus;
            set
            {
                this.replyStatus = value;
                this.accessSubject.OnNext(value);
            }
        }


        public void PingPosition(double latitude, double longitude)
            => this.ReadingSubject.OnNext(new GpsReading
            {
                Position = new Position(latitude, longitude)
            });


        public AccessState GetCurrentStatus(GpsRequest request) => this.ReplyStatus;
        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request) => this.accessSubject;

        public IGpsReading? LastGpsReading { get; set; }
        public IObservable<IGpsReading?> GetLastReading() => Observable.Return(this.LastGpsReading);
        public AccessState RequestAccessReply { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess(GpsRequest request) => Task.FromResult(this.RequestAccessReply);


        public Task StartListener(GpsRequest? request = null)
        {
            this.CurrentListener = request ?? GpsRequest.Realtime(false);
            return Task.CompletedTask;
        }


        public Task StopListener()
        {
            this.CurrentListener = null;
            return Task.CompletedTask;
        }


        public Subject<IGpsReading> ReadingSubject { get; } = new Subject<IGpsReading>();
        public GpsRequest? CurrentListener { get; private set; }
        public IObservable<IGpsReading> WhenReading() => this.ReadingSubject;
    }
}
