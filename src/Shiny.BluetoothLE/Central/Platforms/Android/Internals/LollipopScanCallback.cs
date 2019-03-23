using System;
using System.Collections.Generic;
using Android.Bluetooth.LE;
using SR = Android.Bluetooth.LE.ScanResult;


namespace Shiny.BluetoothLE.Central.Internals
{
    public class LollipopScanCallback : ScanCallback
    {
        readonly Action<SR> callback;


        public LollipopScanCallback(Action<SR> callback)
            => this.callback = callback;


        public override void OnScanResult(ScanCallbackType callbackType, SR result)
            => this.callback(result);


        public override void OnBatchScanResults(IList<SR> results)
        {
            if (results == null)
                return;

            foreach (SR result in results)
                this.callback(result);
        }
    }
}