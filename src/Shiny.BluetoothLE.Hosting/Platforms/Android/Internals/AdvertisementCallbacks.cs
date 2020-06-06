using System;
using Android.Bluetooth.LE;


namespace Shiny.BluetoothLE.Peripherals.Internals
{
    public class AdvertisementCallbacks : AdvertiseCallback
    {
        public Action Started { get; set; }
        public Action<Exception> Failed { get; set; }


        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);
            this.Started?.Invoke();
        }


        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);
            this.Failed?.Invoke(new ArgumentException($"Failed to start BLE advertising - {errorCode}"));
        }
    }
}
