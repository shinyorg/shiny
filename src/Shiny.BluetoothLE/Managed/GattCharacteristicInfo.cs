using System;


namespace Shiny.BluetoothLE.Managed
{
    public class GattCharacteristicInfo
    {
        public GattCharacteristicInfo(string serviceUuid, string characteristicUuid)
        {
            this.ServiceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            this.CharacteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }


        public string ServiceUuid { get; }
        public string CharacteristicUuid { get; }

        public bool IsNotificationsEnabled { get; internal set; }
        public bool UseIndicationIfAvailable { get; internal set; }
        public byte[]? Value { get; internal set; }
        public IGattCharacteristic? Characteristic { get; internal set; }


        public override string ToString() => $"[Service={this.ServiceUuid}, Characteristic={this.CharacteristicUuid}]";
        public bool Equals(string serviceUuid, string characteristicUuid) =>
            String.Equals(this.ServiceUuid, serviceUuid, StringComparison.CurrentCultureIgnoreCase) &&
            String.Equals(this.CharacteristicUuid, characteristicUuid, StringComparison.CurrentCultureIgnoreCase);

        public bool Equals(GattCharacteristicInfo other) => other.ServiceUuid != null && other.CharacteristicUuid != null && this.Equals(other.ServiceUuid, other.CharacteristicUuid);
        public static bool operator ==(GattCharacteristicInfo left, GattCharacteristicInfo right) => Equals(left, right);
        public static bool operator !=(GattCharacteristicInfo left, GattCharacteristicInfo right) => !Equals(left, right);
        public override bool Equals(object obj) => obj is GattCharacteristicInfo info && this.Equals(info);
        public override int GetHashCode() => (this.ServiceUuid, this.CharacteristicUuid).GetHashCode();
    }
}
