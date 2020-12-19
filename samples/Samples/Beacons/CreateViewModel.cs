using System;
using System.Windows.Input;
using System.Reactive.Linq;
using Shiny;
using Shiny.Beacons;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Infrastructure;


namespace Samples.Beacons
{
    public class CreateViewModel : ViewModel
    {
        public CreateViewModel(INavigationService navigator,
                               IDialogs dialogs,
                               IBeaconRangingManager rangingManager,
                               IBeaconMonitoringManager? monitorManager = null)
        {
            this.IsMonitoringSupported = monitorManager != null;

            this.EstimoteDefaults = ReactiveCommand.Create(() =>
            {
                this.Identifier = "Estimote";
                this.Uuid = Constants.EstimoteUuid;
            });

            this.WhenAnyValue(x => x.Major)
                .Select(x => !x.IsEmpty() && UInt16.TryParse(x, out _))
                .ToPropertyEx(this, x => x.IsMajorSet);

            this.StartMonitoring = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var result = await monitorManager.RequestAccess();
                    if (result != AccessState.Available)
                        await dialogs.AlertAccess(result);
                    else
                    {
                        await monitorManager.StartMonitoring(this.GetBeaconRegion());
                        await navigator.GoBack();
                    }
                },
                this.WhenAny(
                    x => x.Identifier,
                    x => x.Uuid,
                    x => x.Major,
                    x => x.Minor,
                    x => x.NotifyOnEntry,
                    x => x.NotifyOnExit,
                    (idValue, uuidValue, majorValue, minorValue, entry, exit) =>
                    {
                        if (monitorManager == null)
                            return false;

                        var atLeast1Notification = entry.GetValue() || exit.GetValue();
                        if (!atLeast1Notification)
                            return false;

                        return this.IsValid();
                    }
                )
            );


            this.StartRanging = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var result = await rangingManager.RequestAccess();
                    if (result != AccessState.Available)
                        await dialogs.AlertAccess(result);
                    else
                    {
                        var region = this.GetBeaconRegion();
                        await navigator.GoBack(false, (nameof(BeaconRegion), region));
                    }
                },
                this.WhenAny(
                    x => x.Identifier,
                    x => x.Uuid,
                    x => x.Major,
                    x => x.Minor,
                    (idValue, uuidValue, majorValue, minorValue) => this.IsValid()
                )
            );
        }



        public bool IsMajorSet { [ObservableAsProperty] get; }

        public ICommand StartMonitoring { get; }
        public ICommand StartRanging { get; }
        public ICommand EstimoteDefaults { get; }
        [Reactive] public string Identifier { get; set; }
        [Reactive] public string Uuid { get; set; }
        [Reactive] public string Major { get; set; }
        [Reactive] public string Minor { get; set; }
        [Reactive] public bool NotifyOnEntry { get; set; } = true;
        [Reactive] public bool NotifyOnExit { get; set; } = true;
        public bool IsMonitoringSupported { get; }


        BeaconRegion GetBeaconRegion() => new BeaconRegion(
            this.Identifier,
            Guid.Parse(this.Uuid),
            GetNumberAddress(this.Major),
            GetNumberAddress(this.Minor)
        )
        {
            NotifyOnEntry = this.NotifyOnEntry,
            NotifyOnExit = this.NotifyOnExit
        };


        bool IsValid()
        {
            if (this.Identifier.IsEmpty())
                return false;

            if (!this.Uuid.IsEmpty() && !Guid.TryParse(this.Uuid, out _))
                return false;

            if (!ValidateNumberAddress(this.Major))
                return false;

            if (!ValidateNumberAddress(this.Minor))
                return false;

            return true;
        }


        static bool ValidateNumberAddress(string value)
        {
            if (value.IsEmpty())
                return true;

            return UInt16.TryParse(value, out _);
        }


        ushort? GetNumberAddress(string value)
        {
            if (value.IsEmpty())
                return null;

            return UInt16.Parse(value);
        }
    }
}
