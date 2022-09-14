using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Shiny;
using Shiny.Locations;
using Xamarin.Forms;


namespace Sample
{
    public class GpsViewModel : SampleViewModel
    {
        readonly IGpsManager manager;
        CompositeDisposable disposer;


        public GpsViewModel()
        {
            this.manager = ShinyHost.Resolve<IGpsManager>();

            var l = this.manager.CurrentListener;
            this.IsUpdating = l != null;

            var mode = l?.BackgroundMode ?? GpsBackgroundMode.None;
            this.UseBackground = mode != GpsBackgroundMode.None;
            this.UseRealtime = mode == GpsBackgroundMode.Realtime;
            this.Accuracy = l?.Accuracy ?? GpsAccuracy.Normal;

            this.NotificationTitle = manager.Title;
            this.NotificationMessage = manager.Message;

            this.GetCurrentPosition = this.CreateOneReading(LocationRetrieve.Current);
            this.GetLastReading = this.CreateOneReading(LocationRetrieve.Last);
            this.GetLastOrCurrent = this.CreateOneReading(LocationRetrieve.LastOrCurrent);

            this.SelectAccuracy = new Command(async () =>
            {
                var choice = await this.Choose(
                    "Select Accuracy",
                    GpsAccuracy.Highest.ToString(),
                    GpsAccuracy.High.ToString(),
                    GpsAccuracy.Normal.ToString(),
                    GpsAccuracy.Low.ToString(),
                    GpsAccuracy.Lowest.ToString()
                );
                this.Accuracy = (GpsAccuracy)Enum.Parse(typeof(GpsAccuracy), choice);
            });


            this.ToggleUpdates = new Command(
                async () =>
                {
                    if (this.manager.CurrentListener != null)
                    {
                        await this.manager.StopListener();
                    }
                    else
                    {
                        var access = await this.manager.RequestAccess(new GpsRequest
                        {
                            BackgroundMode = this.GetMode()
                        });
                        this.Access = access.ToString();

                        if (access != AccessState.Available)
                        {
                            await this.Alert("Insufficient permissions - " + access);
                            return;
                        }

                        var request = new GpsRequest
                        {
                            BackgroundMode = this.GetMode(),
                            Accuracy = this.Accuracy,
                        };
                        try
                        {
                            await this.manager.StartListener(request);
                        }
                        catch (Exception ex)
                        {
                            await this.Alert(ex.ToString());
                        }
                    }
                    this.IsUpdating = this.manager.CurrentListener != null;
                }
            );

            this.RequestAccess = new Command(async () =>
            {
                var request = new GpsRequest { BackgroundMode = this.GetMode() };
                this.Access = (await this.manager.RequestAccess(request)).ToString();
            });
        }


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.disposer = new CompositeDisposable();

            this.manager
                .WhenReading()
                .SubOnMainThread(this.SetValues)
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.IsUpdating)
                .Select(x => x ? "Stop Listening" : "Start Updating")
                .Subscribe(x => this.ListenerText = x)
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.NotificationTitle)
                .Skip(1)
                .Subscribe(x => this.manager.Title = x)
                .DisposedBy(this.disposer);

            this.WhenAnyProperty(x => x.NotificationMessage)
                .Skip(1)
                .Subscribe(x => this.manager.Message = x)
                .DisposedBy(this.disposer);

            this.WhenAnyProperty()
                .Skip(1)
                .Subscribe(_ => this.ToggleUpdates.ChangeCanExecute())
                .DisposedBy(this.disposer);
        }


        public override void OnDisappearing()
        {
            base.OnDisappearing();
            this.disposer?.Dispose();
        }


        public Command SelectAccuracy { get; }
        public Command GetLastReading { get; }
        public Command GetCurrentPosition { get; }
        public Command GetLastOrCurrent { get; }
        public Command RequestAccess { get; }
        public Command ToggleUpdates { get; }

        public bool IsAndroid => ShinyHost.Resolve<IPlatform>().IsAndroid();


        string listenText;
        public string ListenerText
        {
            get => this.listenText;
            private set => this.Set(ref this.listenText, value);
        }


        string nTitle;
        public string NotificationTitle
        {
            get => this.nTitle;
            set => this.Set(ref this.nTitle, value);
        }

        string nMsg;
        public string NotificationMessage
        {
            get => this.nMsg;
            set => this.Set(ref this.nMsg, value);
        }

        bool useBg = true;
        public bool UseBackground
        {
            get => this.useBg;
            set => this.Set(ref this.useBg, value);
        }


        bool useRealtime = true;
        public bool UseRealtime
        {
            get => this.useRealtime;
            set => this.Set(ref this.useRealtime, value);
        }


        GpsAccuracy accuracy = GpsAccuracy.Normal;
        public GpsAccuracy Accuracy
        {
            get => this.accuracy;
            set => this.Set(ref this.accuracy, value);
        }

        string access;
        public string Access
        {
            get => this.access;
            private set => this.Set(ref this.access, value);
        }

        bool updating;
        public bool IsUpdating
        {
            get => this.updating;
            private set => this.Set(ref this.updating, value);
        }

        double lat;
        public double Latitude
        {
            get => this.lat;
            private set => this.Set(ref this.lat, value);
        }

        double lng;
        public double Longitude
        {
            get => this.lng;
            private set => this.Set(ref this.lng, value);
        }

        double alt;
        public double Altitude
        {
            get => this.alt;
            private set => this.Set(ref this.alt, value);
        }

        double pAcc;
        public double PositionAccuracy
        {
            get => this.pAcc;
            private set => this.Set(ref this.pAcc, value);
        }

        double heading;
        public double Heading
        {
            get => this.heading;
            private set => this.Set(ref this.heading, value);
        }

        double hAcc;
        public double HeadingAccuracy
        {
            get => this.hAcc;
            private set => this.Set(ref this.hAcc, value);
        }

        double speed;
        public double Speed
        {
            get => this.speed;
            private set => this.Set(ref this.speed, value);
        }

        DateTime timestamp;
        public DateTime Timestamp
        {
            get => this.timestamp;
            private set => this.Set(ref this.timestamp, value);
        }


        void SetValues(IGpsReading reading)
        {
            this.Latitude = reading.Position.Latitude;
            this.Longitude = reading.Position.Longitude;
            this.Altitude = reading.Altitude;
            this.PositionAccuracy = reading.PositionAccuracy;

            this.Heading = reading.Heading;
            this.HeadingAccuracy = reading.HeadingAccuracy;
            this.Speed = reading.Speed;
            this.Timestamp = reading.Timestamp;
        }


        GpsBackgroundMode GetMode()
        {
            var mode = GpsBackgroundMode.None;
            if (this.UseBackground)
            {
                mode = this.UseRealtime
                    ? GpsBackgroundMode.Realtime
                    : GpsBackgroundMode.Standard;
            }
            return mode;
        }

        static bool IsNumeric(string value)
        {
            if (value.IsEmpty())
                return false;

            if (Int32.TryParse(value, out var r))
                return r > 0;

            return false;
        }


        static TimeSpan ToInterval(string value)
        {
            var i = Int32.Parse(value);
            var ts = TimeSpan.FromSeconds(i);
            return ts;
        }


        Command CreateOneReading(LocationRetrieve retrieve) => new Command(async () =>
        {
            var observable = retrieve switch
            {
                LocationRetrieve.Last => this.manager.GetLastReading(),
                LocationRetrieve.Current => this.manager.GetCurrentPosition(),
                _ => this.manager.GetLastReadingOrCurrentPosition()
            };
            var reading = await observable.ToTask();

            if (reading == null)
                await this.Alert("Could not getting GPS coordinates");
            else
                this.SetValues(reading);

            try
            {
                await this.manager.GetLastReadingOrCurrentPosition().ToTask();
                await Task.Delay(2000).ConfigureAwait(false);
                await this.manager.GetLastReadingOrCurrentPosition().ToTask();
                Console.WriteLine("success");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });


        enum LocationRetrieve
        {
            Last,
            Current,
            LastOrCurrent
        }
    }
}
