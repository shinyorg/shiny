using System;


namespace Shiny.BluetoothLE
{
    static class MessageBusNames
    {
        public const string PeripheralConnected = nameof(PeripheralConnected);
        public const string PeripheralDisconnected = nameof(PeripheralDisconnected);
        public const string PeripheralBondState = nameof(PeripheralBondState);
        public const string PeripheralNameChanged = nameof(PeripheralNameChanged);
        public const string PeripheralPairingRequest = nameof(PeripheralPairingRequest);

        public const string AdapterStateChanged = nameof(AdapterStateChanged);
    }
}
