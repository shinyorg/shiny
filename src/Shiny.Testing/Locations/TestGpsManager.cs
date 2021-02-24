using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Locations;


namespace Shiny.Testing.Locations
{
    public class TestGpsManager : NotifyPropertyChanged, IGpsManager
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


        string? title;
        public string? Title
        {
            get => this.title;
            set => this.Set(ref this.title, value);
        }


        string? message;
        public string? Message
        {
            get => this.message;
            set => this.Set(ref this.message, value);
        }


        bool indeterministic;
        public bool IsIndeterministic
        {
            get => this.indeterministic;
            set => this.Set(ref this.indeterministic, value);
        }


        int progress;
        public int Progress
        {
            get => this.progress;
            set => this.Set(ref this.progress, value);
        }


        int total;
        public int Total
        {
            get => this.total;
            set => this.Set(ref this.total, value);
        }


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
