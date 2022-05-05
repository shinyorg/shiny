using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Foundation;
using UIKit;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        CompositeDisposable? dispose;


        public bool IsEnergySavingEnabled => NSProcessInfo.ProcessInfo.LowPowerModeEnabled;


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                this.dispose = new CompositeDisposable(
                    this.WhenBatteryPercentageChanged()
                        .Subscribe(_ => this.RaisePropertyChanged(nameof(this.BatteryLevel))),

                    this.WhenPowerStatusChanged()
                        .Subscribe(_ => this.RaisePropertyChanged(nameof(this.Status)))
                );
            }
            else
            {
                this.dispose?.Dispose();
            }
        }


        public int BatteryLevel => Math.Abs((int)(UIDevice.CurrentDevice.BatteryLevel * 100F));
        public PowerState Status
        {
            get
            {
                switch (UIDevice.CurrentDevice.BatteryState)
                {
                    case UIDeviceBatteryState.Charging:
                        return PowerState.Charging;

                    case UIDeviceBatteryState.Full:
                        return PowerState.Charged;

                    case UIDeviceBatteryState.Unplugged:
                        return PowerState.Discharging;

                    case UIDeviceBatteryState.Unknown:
                    default:
                        return PowerState.Unknown;
                }
            }
        }


        IObservable<int> WhenBatteryPercentageChanged() => Observable.Create<int>(ob =>
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
            var not = UIDevice
                .Notifications
                .ObserveBatteryLevelDidChange((sender, args) => ob.OnNext(this.BatteryLevel));

            return () =>
            {
                UIDevice.CurrentDevice.BatteryMonitoringEnabled = false;
                not.Dispose();
            };
        });


        IObservable<PowerState> WhenPowerStatusChanged() => Observable.Create<PowerState>(ob =>
            UIDevice
                .Notifications
                .ObserveBatteryStateDidChange((sender, args) => ob.OnNext(this.Status))
        );
    }
}
