using System;


namespace Shiny.BluetoothLE.Managed
{
    public class GattCharacteristicInfo
    {
        public GattCharacteristicInfo(string serviceUuid, string characteristicUuid)
        {
            this.ServiceUuid = serviceUuid;
            this.CharacteristicUuid = characteristicUuid;
        }


        public string ServiceUuid { get; }
        public string CharacteristicUuid { get; }

        public bool IsNotificationsEnabled { get; internal set; }
        public bool UseIndicationIfAvailable { get; internal set; }
        public byte[]? Value { get; internal set; }
        public IGattCharacteristic? Characteristic { get; internal set; }


        public override string ToString() => $"[Service={this.ServiceUuid}, Characteristic={this.CharacteristicUuid}]";
        public bool Equals(string serviceUuid, string characteristicUuid) => (this.ServiceUuid, this.CharacteristicUuid) == (serviceUuid, characteristicUuid);
        public bool Equals(GattCharacteristicInfo other) => this.Equals(other?.ServiceUuid, other?.CharacteristicUuid);
        public static bool operator ==(GattCharacteristicInfo left, GattCharacteristicInfo right) => Equals(left, right);
        public static bool operator !=(GattCharacteristicInfo left, GattCharacteristicInfo right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is GattCharacteristicInfo info && this.Equals(info);
        public override int GetHashCode() => (this.ServiceUuid, this.CharacteristicUuid).GetHashCode();
    }
}
