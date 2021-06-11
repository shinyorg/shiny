using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.App;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE
{
    [BroadcastReceiver(
        Name = "com.shiny.bluetoothle.ShinyBleAdapterStateBroadcastReceiver",
        Enabled = true,
        Exported = true
    )]
    public class ShinyBleAdapterStateBroadcastReceiver : BroadcastReceiver
    {
        static readonly Subject<State> stateSubj = new Subject<State>();
        public static IObservable<State> WhenStateChanged() => stateSubj;


        public override void OnReceive(Context? context, Intent? intent)
        {
            switch (intent?.Action)
            {
                case BluetoothAdapter.ActionStateChanged:
                    var newState = (State)intent.GetIntExtra(BluetoothAdapter.ExtraState, -1);
                    stateSubj.OnNext(newState);
                    break;
            }
        }
    }
}
