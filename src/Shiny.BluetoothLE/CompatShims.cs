using System;
using Shiny.BluetoothLE.Central;


namespace Shiny.BluetoothLE
{
    public static class CrossBleAdapter
    {
        public static ICentralManager Current => ShinyHost.Resolve<ICentralManager>();
    }
}
