using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Characteristic = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;


namespace Shiny.BluetoothLE
{
    public static class Utils
    {
        public static string ToUuidString(string value)
        {
            if (value.Length == 4)
                value = $"0000{value}-0000-1000-8000-00805F9B34FB";

            return value;
        }


        public static Guid ToUuidType(string value)
            => Guid.Parse(ToUuidString(value));


        public static async Task Execute(this IAsyncOperation<GattCommunicationStatus> operation, CancellationToken ct)
        {
            var result = await operation.AsTask(ct).ConfigureAwait(false);
            result.Assert();
        }


        public static void Assert(this GattCommunicationStatus status)
        {
            if (status != GattCommunicationStatus.Success)
                throw new ArgumentException("Invalid Communcation Status - " + status);
        }


        public static bool HasNotify(this Characteristic ch) =>
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate) ||
            ch.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);


        public static ulong ToBluetoothAddress(this Guid deviceId)
        {
            var address = deviceId
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray();

            var hexAddress = BitConverter.ToString(address).Replace("-", "");
            if (ulong.TryParse(hexAddress, NumberStyles.HexNumber, null, out var mac))
                return mac;

            return 0L;
        }



        static readonly Regex macRegex = new Regex("(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})");
        const string REGEX_REPLACE = "$1:$2:$3:$4:$5:$6";


        static string ToMacAddress(ulong address)
        {
            var tempMac = address.ToString("X");
            //tempMac is now 'E7A1F7842F17'

            //string.Join(":", BitConverter.GetBytes(BluetoothAddress).Reverse().Select(b => b.ToString("X2"))).Substring(6);
            var leadingZeros = new string('0', 12 - tempMac.Length);
            tempMac = leadingZeros + tempMac;

            var macAddress = macRegex.Replace(tempMac, REGEX_REPLACE);
            return macAddress;
        }


        public static Guid GetDeviceId(this BluetoothLEDevice native)
        {
            var address = ToMacAddress(native.BluetoothAddress);
            var mac = address
                .Replace("BluetoothLE#BluetoothLE", String.Empty)
                .Replace(":", String.Empty)
                .Replace("-", String.Empty);

            var deviceGuid = new byte[16];
            var macBytes = Enumerable
                .Range(0, mac.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(mac.Substring(x, 2), 16))
                .ToArray();

            macBytes.CopyTo(deviceGuid, 10); // 12 bytes here if off the BluetoothLEDevice
            return new Guid(deviceGuid);
        }
    }
}
