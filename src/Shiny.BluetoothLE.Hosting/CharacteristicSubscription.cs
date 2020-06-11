using System;


namespace Shiny.BluetoothLE.Hosting
{
    public class CharacteristicSubscription
    {
        public CharacteristicSubscription(IGattCharacteristic characteristic,
                                          IPeripheral peripheral,
                                          bool isSubscribing)
        {
            this.Characteristic = characteristic;
            this.Peripheral = peripheral;
            this.IsSubscribing = isSubscribing;
        }



        public IGattCharacteristic Characteristic { get; }
        public IPeripheral Peripheral { get; }
        public bool IsSubscribing { get; }
    }
}
