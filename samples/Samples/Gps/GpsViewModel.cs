using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.Locations;


namespace Samples.Gps
{
    public class GpsViewModel : ViewModel
    {
        readonly IGpsManager manager;
        IDisposable gpsListener;


        public GpsViewModel(IGpsManager manager, IDialogs dialogs)
        {
            this.manager = manager;

            var l = this.manager.CurrentListener;
            this.IsUpdating = l != null;
            this.UseBackground = l?.UseBackground ?? true;
            this.Priority = l?.Priority ?? GpsPriority.Normal;
            this.DesiredInterval = l?.Interval.TotalSeconds.ToString() ?? String.Empty;
            this.ThrottledInterval = l?.ThrottledInterval?.TotalSeconds.ToString() ?? String.Empty;
            this.MinimumDistanceMeters = l?.MinimumDistance?.TotalMeters.ToString() ?? String.Empty;

            this.WhenAnyValue(x => x.UseBackground)
                .Subscribe(x => this.Access = this.manager.GetCurrentStatus(
                    new GpsRequest { UseBackground = this.UseBackground }).ToString()
                );

            this.WhenAnyValue(x => x.IsUpdating)
                .Select(x => x ? "Stop Listening" : "Start Updating")
                .ToPropertyEx(this, x => x.ListenerText);

            this.GetCurrentPosition = ReactiveCommand.CreateFromTask(async _ =>
            {
                var result = await dialogs.RequestAccess(() => this.manager.RequestAccess(new GpsRequest()));
                if (!result)
                    return;

                var reading = await this.manager.GetLastReading();
                if (reading == null)
                    await dialogs.Alert("Could not getting GPS coordinates");
                else
                    this.SetValues(reading);
            });
            this.BindBusyCommand(this.GetCurrentPosition);

            ReactiveCommand.Create(() => dialogs.ActionSheet(
                "Select Priority",
                false,
                ("Highest", () => this.Priority = GpsPriority.Highest),
                ("Normal", () => this.Priority = GpsPriority.Normal),
                ("Low", () => this.Priority = GpsPriority.Low)
            ));

            this.ToggleUpdates = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    if (this.manager.CurrentListener != null)
                    {
                        await this.manager.StopListener();
                        this.gpsListener?.Dispose();
                    }
                    else
                    {
                        var result = await dialogs.RequestAccess(() => this.manager.RequestAccess(new GpsRequest
                        {
                            UseBackground = this.UseBackground
                        }));
                        if (!result)
                        {
                            await dialogs.Alert("Insufficient permissions");
                            return;
                        }

                        var request = new GpsRequest
                        {
                            UseBackground = this.UseBackground,
                            Priority = this.Priority,
                        };
                        if (IsNumeric(this.DesiredInterval))
                            request.Interval = ToInterval(this.DesiredInterval);

                        if (IsNumeric(this.ThrottledInterval))
                            request.ThrottledInterval = ToInterval(this.ThrottledInterval);

                        if (IsNumeric(this.MinimumDistanceMeters))
                            request.MinimumDistance = Distance.FromMeters(Int32.Parse(this.MinimumDistanceMeters));

                        await this.manager.StartListener(request);
                    }
                    this.IsUpdating = this.manager.CurrentListener != null;
                },
                this.WhenAny(
                    x => x.IsUpdating,
                    x => x.DesiredInterval,
                    x => x.ThrottledInterval,
                    x => x.MinimumDistanceMeters,
                    (u, i, t, d) =>
                    {
                        if (u.GetValue())
                            return true;

                        var isdesired = IsNumeric(i.GetValue());
                        var isthrottled = IsNumeric(t.GetValue());
                        var ismindist = IsNumeric(d.GetValue());

                        if (isdesired && isthrottled)
                        {
                            var desired = ToInterval(i.GetValue());
                            var throttle = ToInterval(t.GetValue());
                            if (throttle.TotalSeconds >= desired.TotalSeconds)
                                return false;
                        }
                        return true;
                    }
                )
            );

            this.UseRealtime = ReactiveCommand.Create(() =>
            {
                var rt = GpsRequest.Realtime(false);
                this.ThrottledInterval = String.Empty;
                this.DesiredInterval = rt.Interval.TotalSeconds.ToString();
                this.Priority = rt.Priority;
            });

            this.RequestAccess = ReactiveCommand.CreateFromTask(async () =>
            {
                var access = await this.manager.RequestAccess(new GpsRequest { UseBackground = this.UseBackground });
                this.Access = access.ToString();
            });
            this.BindBusyCommand(this.RequestAccess);
        }


        public override void OnAppearing()
        {
            base.OnAppearing();

            this.gpsListener = this.manager
                .WhenReading()
                .SubOnMainThread(this.SetValues)
                .DisposeWith(this.DeactivateWith);
        }


        void SetValues(IGpsReading reading)
        {
            using (this.DelayChangeNotifications())
            {
                this.Latitude = reading.Position.Latitude;
                this.Longitude = reading.Position.Longitude;
                this.Altitude = reading.Altitude;
                this.PositionAccuracy = reading.PositionAccuracy;

                this.Heading = reading.Heading;
                this.HeadingAccuracy = reading.HeadingAccuracy;
                this.Speed = reading.Speed;
            }
        }


        public IReactiveCommand UseRealtime { get; }
        public IReactiveCommand SelectPriority { get; }
        public IReactiveCommand GetCurrentPosition { get; }
        public IReactiveCommand ToggleUpdates { get; }
        public IReactiveCommand RequestAccess { get; }


        public string ListenerText { [ObservableAsProperty] get; }

        [Reactive] public bool UseBackground { get; set; } = true;
        [Reactive] public GpsPriority Priority { get; set; } = GpsPriority.Normal;
        [Reactive] public string DesiredInterval { get; set; }
        [Reactive] public string ThrottledInterval { get; set; }
        [Reactive] public string MinimumDistanceMeters { get; set; }
        [Reactive] public string Access { get; private set; }
        [Reactive] public bool IsUpdating { get; private set; }
        [Reactive] public double Latitude { get; private set; }
        [Reactive] public double Longitude { get; private set; }
        [Reactive] public double Altitude { get; private set; }
        [Reactive] public double PositionAccuracy { get; private set; }
        [Reactive] public double Heading { get; private set; }
        [Reactive] public double HeadingAccuracy { get; private set; }
        [Reactive] public double Speed { get; private set; }


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
    }
}
