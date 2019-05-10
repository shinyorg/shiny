using System;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Central
{
    public static partial class Extensions
    {
        public static AccessState FromNative(this CBCentralManagerState state)
        {
            switch (state)
            {
                case CBCentralManagerState.PoweredOff:
                    return AccessState.Disabled;

                case CBCentralManagerState.Resetting:
                case CBCentralManagerState.PoweredOn:
                    return AccessState.Available;

                case CBCentralManagerState.Unauthorized:
                    return AccessState.Denied;

                case CBCentralManagerState.Unsupported:
                    return AccessState.NotSupported;

                case CBCentralManagerState.Unknown:
                default:
                    return AccessState.Unknown;
            }
        }
    }
}
