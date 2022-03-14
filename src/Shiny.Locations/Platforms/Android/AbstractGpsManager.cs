using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace Shiny.Locations
{
    public abstract partial class AbstractGpsManager : IGpsManager, IShinyStartupTask
    {
        readonly Subject<IGpsReading> readingSubj;
        readonly ILogger logger;


        protected AbstractGpsManager(IPlatform context, ILogger logger)
        {
            this.readingSubj = new Subject<IGpsReading>();
            this.Context = context;
            this.logger = logger;
            this.Callback = new ShinyLocationCallback
            {
                OnReading = x => this.readingSubj.OnNext(new GpsReading(x))
            };
        }


        public virtual async void Start()
        {
            if (this.CurrentListener != null)
            {
                try
                {
                    await this.StartListener(this.CurrentListener).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning("Failed to auto-start GPS", ex);
                }
            }
        }


        protected ShinyLocationCallback Callback { get; }
        protected IPlatform Context { get; }


        GpsRequest? request;
        public GpsRequest? CurrentListener
        {
            get => this.request;
            set
            {
                var bg = value?.BackgroundMode ?? GpsBackgroundMode.None;
                if (bg == GpsBackgroundMode.None)
                    this.request = value;
                else
                    this.Set(ref this.request, value);
            }
        }


        public async Task<AccessState> RequestAccess(GpsRequest request)
        {
            var status = AccessState.Denied;
            var type = request.Precise
                ? LocationPermissionType.Fine
                : LocationPermissionType.Coarse;

            switch (request.BackgroundMode)
            {
                case GpsBackgroundMode.None:
                    status = await this.Context.RequestLocationAccess(type);
                    break;

                case GpsBackgroundMode.Standard:
                    status = await this.Context.RequestBackgroundLocationAccess(type);
                    break;

                case GpsBackgroundMode.Realtime:
                    status = await this.Context.RequestBackgroundLocationAccess(LocationPermissionType.Fine);
                    break;
            }
            return status;
        }


        public virtual IObservable<IGpsReading> WhenReading()
            => this.readingSubj;


        public virtual async Task StartListener(GpsRequest request)
        {
            if (this.CurrentListener != null)
                return;

            request ??= new GpsRequest();
            (await this.RequestAccess(request)).Assert(allowRestricted: true);

            if (request.BackgroundMode == GpsBackgroundMode.Realtime && !ShinyGpsService.IsStarted)
                this.Context.StartService(typeof(ShinyGpsService));

            await this.RequestLocationUpdates(request);
            this.CurrentListener = request;
        }


        public virtual async Task StopListener()
        {
            if (this.CurrentListener == null)
                return;

            await this.RemoveLocationUpdates();
            if (this.CurrentListener.BackgroundMode == GpsBackgroundMode.Realtime && ShinyGpsService.IsStarted)
                this.Context.StopService(typeof(ShinyGpsService));

            this.CurrentListener = null;
        }


        public abstract IObservable<IGpsReading?> GetLastReading();
        protected abstract Task RequestLocationUpdates(GpsRequest request);
        protected abstract Task RemoveLocationUpdates();
    }
}
