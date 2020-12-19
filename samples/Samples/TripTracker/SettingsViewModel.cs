using System;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;
using Shiny;
using Shiny.TripTracker;


namespace Samples.TripTracker
{
    public class SettingsViewModel : ViewModel
    {
        public SettingsViewModel(ITripTrackerManager manager, IDialogs dialogs)
        {
            this.IsEnabled = manager.TrackingType == null;
            this.UseAutomotive = manager.TrackingType == TripTrackingType.Automotive;
            this.UseCycling = manager.TrackingType == TripTrackingType.Cycling;
            this.UseRunning = manager.TrackingType == TripTrackingType.Running;
            this.UseWalking = manager.TrackingType == TripTrackingType.Walking;
            this.UseOnFoot = manager.TrackingType == TripTrackingType.OnFoot;
            this.UseExercise = manager.TrackingType == TripTrackingType.Exercise;
            this.UseStationary = manager.TrackingType == TripTrackingType.Stationary;

            this.ToggleMonitoring = ReactiveCommand.CreateFromTask
            (
                async () =>
                {
                    var access = await manager.RequestAccess();
                    if (access != AccessState.Available)
                    {
                        await dialogs.Alert("Invalid Access - " + access);
                    }
                    else
                    {
                        if (!this.IsEnabled)
                        {
                            await manager.StopTracking();
                        }
                        else
                        {
                            var type = this.GetTrackingType().Value;
                            await manager.StartTracking(type);
                        }
                        this.IsEnabled = !this.IsEnabled;
                        this.RaisePropertyChanged(nameof(this.MonitoringText));
                    }
                },
                this.WhenAny(
                    x => x.UseAutomotive,
                    x => x.UseRunning,
                    x => x.UseWalking,
                    x => x.UseCycling,
                    x => x.UseOnFoot,
                    x => x.UseExercise,
                    x => x.UseStationary,
                    (auto, run, walk, cycle, foot, ex, st) => this.GetTrackingType() != null
                )
            );
        }


        public ICommand ToggleMonitoring { get; }
        public string MonitoringText => this.IsEnabled ? "Start Monitoring" : "Stop Monitoring";
        [Reactive] public bool IsEnabled { get; private set; }
        [Reactive] public bool UseAutomotive { get; set; }
        [Reactive] public bool UseWalking { get; set; }
        [Reactive] public bool UseRunning { get; set; }
        [Reactive] public bool UseCycling { get; set; }
        [Reactive] public bool UseOnFoot { get; set; }
        [Reactive] public bool UseExercise { get; set; }
        [Reactive] public bool UseStationary { get; set; }

        TripTrackingType? GetTrackingType()
        {
            if (this.UseAutomotive)
                return TripTrackingType.Automotive;

            if (this.UseCycling)
                return TripTrackingType.Cycling;

            if (this.UseRunning)
                return TripTrackingType.Running;

            if (this.UseWalking)
                return TripTrackingType.Walking;

            if (this.UseOnFoot)
                return TripTrackingType.OnFoot;

            if (this.UseExercise)
                return TripTrackingType.Exercise;

            if (this.UseStationary)
                return TripTrackingType.Stationary;

            return null;
        }
    }
}
