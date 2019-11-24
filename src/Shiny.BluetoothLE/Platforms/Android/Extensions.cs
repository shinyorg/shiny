using System;
using Android.Bluetooth;
using Shiny.BluetoothLE.Peripherals;
using Android.Content;
using DroidGattStatus = Android.Bluetooth.GattStatus;


namespace Shiny.BluetoothLE
{
    public static class Extensions
    {
        public static AccessState GetAccessState(this BluetoothManager bluetoothManager)
        {
            var ad = bluetoothManager?.Adapter;
            if (ad == null)
                return AccessState.NotSupported;

            if (!ad.IsEnabled)
                return AccessState.Disabled;

            return ad.State.FromNative();
        }


        public static AccessState FromNative(this State state)
        {
            switch (state)
            {
                case State.Off:
                case State.TurningOff:
                case State.Disconnecting:
                case State.Disconnected:
                    return AccessState.Disabled;

                case State.On:
                case State.Connected:
                    return AccessState.Available;

                default:
                    return AccessState.Unknown;
            }
        }


        public static BluetoothManager GetBluetooth(this AndroidContext context)
            => (BluetoothManager)context.AppContext.GetSystemService(Context.BluetoothService);


        public static ConnectionState ToStatus(this ProfileState state)
        {
            switch (state)
            {
                case ProfileState.Connected:
                    return ConnectionState.Connected;

                case ProfileState.Connecting:
                    return ConnectionState.Connecting;

                case ProfileState.Disconnecting:
                    return ConnectionState.Disconnecting;

                case ProfileState.Disconnected:
                default:
                    return ConnectionState.Disconnected;
            }
        }


        public static DroidGattStatus ToNative(this GattState status)
            => (DroidGattStatus)Enum.Parse(typeof(DroidGattStatus), status.ToString());
    }
}