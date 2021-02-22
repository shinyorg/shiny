using System;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Shiny.Locations
{
    public abstract class AbstractGpsManager : NotifyPropertyChanged,
                                               IGpsManager,
                                               IShinyStartupTask
    {

        protected AbstractGpsManager(IAndroidContext context)
        {
            this.Context = context;
            this.Callback = new ShinyLocationCallback();
        }


        public virtual async void Start()
        {
            if (this.CurrentListener?.UseBackground ?? false)
                await this.RequestLocationUpdates(this.CurrentListener);

            this.CurrentListener = null;
        }


        protected ShinyLocationCallback Callback { get; }
        protected IAndroidContext Context { get; }


        //string? title;
        //public string? Title
        //{
        //    get => this.title;
        //    set => this.Set(ref this.title, value);
        //}


        //string? description;
        //public string? Description
        //{
        //    get => this.description;
        //    set => this.Set(ref this.description, value);
        //}


        //string? ticker;
        //public string? Ticker
        //{
        //    get => this.ticker;
        //    set => this.Set(ref this.ticker, value);
        //}


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

        public virtual IObservable<IGpsReading> WhenReading() => this.Callback.ReadingSubject;


        public virtual async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                return;

            request ??= new GpsRequest();
            (await this.RequestAccess(request)).Assert(allowRestricted: true);

            if (request.UseBackground && !ShinyGpsService.IsStarted)
                this.Context.StartService(typeof(ShinyGpsService), true);

            await this.RequestLocationUpdates(request);
            this.CurrentListener = request;
        }


        public virtual async Task StopListener()
        {
            if (this.CurrentListener == null)
                return;

            //await this.client.RemoveLocationUpdatesAsync(this.GetPendingIntent());
            //await this.client.RemoveLocationUpdatesAsync(this.Callback);
            this.Context.StopService(typeof(ShinyGpsService));
            this.CurrentListener = null;
        }


        public abstract IObservable<IGpsReading?> GetLastReading();
        protected abstract Task RequestLocationUpdates(GpsRequest request);
        protected abstract Task RemoveLocationUpdates();
    }
}
