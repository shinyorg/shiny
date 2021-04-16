using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public abstract partial class AbstractGpsManager : IGpsManager, IShinyStartupTask
    {
        readonly Subject<IGpsReading> readingSubj;


        protected AbstractGpsManager(IAndroidContext context)
        {
            this.readingSubj = new Subject<IGpsReading>();
            this.Context = context;
            this.Callback = new ShinyLocationCallback
            {
                OnReading = x => this.readingSubj.OnNext(new GpsReading(x))
            };
        }


        public virtual async void Start()
        {
            if (this.CurrentListener?.UseBackground ?? false)
                await this.StartListener(this.CurrentListener);
            else
                this.CurrentListener = null;
        }


        protected ShinyLocationCallback Callback { get; }
        protected IAndroidContext Context { get; }


        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set => this.Set(ref this.request, value);
        }


        public IObservable<AccessState> WhenAccessStatusChanged(GpsRequest request)
            => Observable.Interval(TimeSpan.FromSeconds(2)).Select(_ => this.GetCurrentStatus(request));

        public AccessState GetCurrentStatus(GpsRequest request)
            => this.Context.GetCurrentLocationAccess(request.UseBackground, true, true, false);

        public Task<AccessState> RequestAccess(GpsRequest request)
            => this.Context.RequestLocationAccess(request.UseBackground, true, true, false);

        public virtual IObservable<IGpsReading> WhenReading()
            => this.readingSubj;


        public virtual async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                return;

            request ??= new GpsRequest();
            (await this.RequestAccess(request)).Assert(allowRestricted: true);

            if (request.UseBackground && !ShinyGpsService.IsStarted)
                this.Context.StartService(typeof(ShinyGpsService));

            await this.RequestLocationUpdates(request);
            this.CurrentListener = request;
        }


        public virtual async Task StopListener()
        {
            if (this.CurrentListener == null)
                return;

            await this.RemoveLocationUpdates();
            if (this.CurrentListener.UseBackground && ShinyGpsService.IsStarted)
                this.Context.StopService(typeof(ShinyGpsService));

            this.CurrentListener = null;
        }


        public abstract IObservable<IGpsReading?> GetLastReading();
        protected abstract Task RequestLocationUpdates(GpsRequest request);
        protected abstract Task RemoveLocationUpdates();
    }
}
