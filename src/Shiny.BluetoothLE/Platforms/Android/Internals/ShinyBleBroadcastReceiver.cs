using System;
using System.Reactive.Subjects;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Internals
{
    [BroadcastReceiver(
        Name = BleManager.BroadcastReceiverName,
        Enabled = true,
        Exported = true
    )]
    public class ShinyBleBroadcastReceiver : BroadcastReceiver
    {
        static readonly Subject<Intent> bleSubj = new Subject<Intent>();
        public static IObservable<Intent> WhenBleEvent() => bleSubj;


        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent != null)
                bleSubj.OnNext(intent);
        }
    }
}
