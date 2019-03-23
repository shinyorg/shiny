using System;
using System.Reactive.Subjects;
using Microsoft.JSInterop;


namespace Acr.BluetoothLE.Central
{
    public static class Callbacks
    {
        public static Subject<IScanResult> ScanResults { get; } = new Subject<IScanResult>();


        [JSInvokable]
        public static void OnScanResult(object device)
        {
            ScanResults.OnNext(null);
        }


        public static Subject<IGattService> Services = new Subject<IGattService>();

        [JSInvokable]
        public static void OnService()
        {

        }


        public static Subject<IGattCharacteristic> Characteristics { get; } = new Subject<IGattCharacteristic>();

        [JSInvokable]
        public static void OnCharacteristic()
        {

        }
    }
}
