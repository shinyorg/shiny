using System;
using Android.Bluetooth.LE;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class AdvertisementCallbacks : AdvertiseCallback
    {
        readonly Action onStart;
        readonly Action<Exception> onError;


        public AdvertisementCallbacks(Action onStart, Action<Exception> onError)
        {
            this.onStart = onStart;
            this.onError = onError;
        }


        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);
            this.onStart.Invoke();
        }


        public override void OnStartFailure(AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);
            this.onError.Invoke(new ArgumentException($"Failed to start BLE advertising - {errorCode}"));
        }
    }
}
