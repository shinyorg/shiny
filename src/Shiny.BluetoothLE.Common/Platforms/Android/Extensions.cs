using Android.Bluetooth;

namespace Shiny.BluetoothLE;


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


    public static AccessState FromNative(this State state) => state switch
    {
        var x when
            x == State.Off ||
            x == State.TurningOff ||
            x == State.Disconnecting ||
            x == State.Disconnected
                => AccessState.Disabled,

        var x when
            x == State.On ||
            x == State.Connected
                => AccessState.Available,

        _ => AccessState.Unknown
    };


    public static ConnectionState ToStatus(this ProfileState state) => state switch
    {
        ProfileState.Connected => ConnectionState.Connected,
        ProfileState.Connecting => ConnectionState.Connecting,
        ProfileState.Disconnecting => ConnectionState.Disconnecting,
        _ => ConnectionState.Disconnected
    };
}