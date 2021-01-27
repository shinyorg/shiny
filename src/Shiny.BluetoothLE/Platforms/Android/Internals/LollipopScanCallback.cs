using System;
using System.Collections.Generic;
using Android.Bluetooth.LE;
using Android.Runtime;
using SR = Android.Bluetooth.LE.ScanResult;


namespace Shiny.BluetoothLE.Internals
{
    public class LollipopScanCallback : ScanCallback
    {
        readonly Action<SR> callback;
        readonly Action<ScanFailure> onScanFailure;


        public LollipopScanCallback(Action<SR> callback, Action<ScanFailure> onScanFailure)
        {
            this.callback = callback;
            this.onScanFailure = onScanFailure;
        }


        public override void OnScanFailed([GeneratedEnum] ScanFailure errorCode)
            => this.onScanFailure(errorCode);


        public override void OnScanResult(ScanCallbackType callbackType, SR result)
            => this.callback(result);


        public override void OnBatchScanResults(IList<SR> results)
        {
            if (results == null)
                return;

            foreach (var result in results)
                this.callback(result);
        }
    }
}