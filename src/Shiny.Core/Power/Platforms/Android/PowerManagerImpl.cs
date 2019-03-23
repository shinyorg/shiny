using System;
using Android.Content;
using Android.OS;


namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        readonly IAndroidContext context;
        IDisposable dispose;


        public PowerManagerImpl(IAndroidContext context)
        {
            this.context = context;
        }


        protected override void OnNpcHookChanged(bool hasSubscribers)
        {
            if (hasSubscribers)
            {
                this.dispose = this.context
                    .WhenIntentReceived(Intent.ActionBatteryChanged)
                    .Subscribe(_ =>
                    {
                        this.OnPropertyChanged(nameof(this.Status));
                        this.OnPropertyChanged(nameof(this.BatteryLevel));
                    });
            }
            else
            {
                this.dispose?.Dispose();
            }
        }


        public PowerState Status => this.context.GetIntentValue(Intent.ActionBatteryChanged, ToState);
        public int BatteryLevel => this.context.GetIntentValue(Intent.ActionBatteryChanged, ToLevel);


        static PowerState ToState(Intent intent)
        {
            var status = (BatteryStatus)intent.GetIntExtra(BatteryManager.ExtraStatus, -1);
            switch (status)
            {
                case BatteryStatus.Discharging:
                case BatteryStatus.NotCharging:
                    return PowerState.Discharging;

                case BatteryStatus.Charging:
                    return PowerState.Charging;

                case BatteryStatus.Full:
                    return PowerState.Charged;

                default:
                    return PowerState.Unknown;
            }
        }


        static int ToLevel(Intent intent)
        {
            var level = intent.GetIntExtra(BatteryManager.ExtraLevel, -1);
            var scale = intent.GetIntExtra(BatteryManager.ExtraScale, -1);

            return (int) Math.Floor(level * 100D / scale);
        }
    }
}