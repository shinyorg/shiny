using System;
using Android.App;
using Android.Content;
using Android.OS;

[assembly: UsesPermission(Android.Manifest.Permission.BatteryStats)]

namespace Shiny.Power
{
    public class PowerManagerImpl : NotifyPropertyChanged, IPowerManager
    {
        readonly AndroidContext context;
        IDisposable? dispose;


        public PowerManagerImpl(AndroidContext context)
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
                        this.RaisePropertyChanged(nameof(this.Status));
                        this.RaisePropertyChanged(nameof(this.BatteryLevel));
                    });
            }
            else
            {
                this.dispose?.Dispose();
            }
        }


        ////<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS"/>
        //public void RequestIgnoreBatteryOptimizations()
        //{
        //    //Intent intent = new Intent();
        //    //String packageName = context.getPackageName();
        //    //PowerManager pm = (PowerManager)context.getSystemService(Context.POWER_SERVICE);
        //    //if (pm.isIgnoringBatteryOptimizations(packageName))
        //    //    intent.setAction(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS);
        //    //else
        //    //{
        //    //    intent.setAction(Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS);
        //    //    intent.setData(Uri.parse("package:" + packageName));
        //    //}
        //    //context.startActivity(intent);
        //    //myIntent.setAction(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS); // better

        //    var intent = new Intent("android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS");
        //    intent.SetData(Android.Net.Uri.Parse("package:" + this.context.Package.PackageName));
        //    this.context.AppContext.StartActivity(intent);
        //}


        public bool IsEnergySavingEnabled => this.context
            .GetSystemServiceValue<bool, PowerManager>(
                Context.PowerService,
                pm =>
                    pm.IsDeviceIdleMode &&
                    !pm.IsIgnoringBatteryOptimizations(this.context.Package.PackageName)
            );
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