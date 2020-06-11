using System;
using Shiny.BluetoothLE;


namespace Shiny
{
    public static class ShinyBLE
    {
        public static IBleManager Central { get; } = ShinyHost.Resolve<IBleManager>();
    }
}
