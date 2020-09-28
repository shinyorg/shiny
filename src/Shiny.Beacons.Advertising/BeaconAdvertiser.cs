using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;


namespace Shiny.Beacons.Advertising
{
    public class BeaconAdvertiser : IBeaconAdvertiser
    {
        readonly IBleHostingManager manager;
        public BeaconAdvertiser(IBleHostingManager manager) => this.manager = manager;

        public Guid? Uuid { get; private set; }
        public ushort? Major { get; private set; }
        public ushort? Minor { get; private set; }
        public int? TxPower { get; private set; }


        public async Task Start(Guid uuid, ushort major, ushort minor, int txpower = 0)
        {
            if (this.Uuid != null)
                throw new ArgumentException("Beacon Advertiser is already active");

            if (this.manager.IsAdvertising)
                throw new ArgumentException("BLE is already advertising");

            this.Uuid = uuid;
            this.Major = major;
            this.Minor = minor;
            this.TxPower = txpower;

            await this.manager.StartAdvertising(new AdvertisementData
            {
                //= new ManufacturerData(76, this.GetBeaconPacket());
                //= new ManufacturerData { Data = this.GetBeaconPacket() };
                ManufacturerData = this.GetBeaconPacket()
            });
        }


        public Task Stop()
        {
            this.Uuid = null;
            this.Major = null;
            this.Minor = null;
            this.TxPower = null;

            this.manager.StopAdvertising();
            return Task.CompletedTask;
        }


        ManufacturerData GetBeaconPacket()
        {
            using (var ms = new MemoryStream())
            {
                using (var br = new BinaryWriter(ms))
                {
                    //br.Write(76);
                    br.Write(new byte[] { 0, 0, 0 });
                    br.Write(ToBytes(this.Uuid.Value));
                    br.Write(BitConverter.GetBytes(this.Major.Value).Reverse().ToArray());
                    br.Write(BitConverter.GetBytes(this.Minor.Value).Reverse().ToArray());
                    br.Write(this.TxPower.Value);
                }

                //return ms.ToArray();
                return new ManufacturerData(76, ms.ToArray());
            }
        }


        static byte[] ToBytes(Guid guid)
        {
            var hex = guid
                .ToString()
                .Replace("-", String.Empty)
                .Replace("{", String.Empty)
                .Replace("}", String.Empty)
                .Replace(":", String.Empty)
                .Replace("-", String.Empty);

            var bytes = Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();

            return bytes;
        }
    }
}
// on iOS, there is a slightly different mechanic
//            func advertiseDevice(region : CLBeaconRegion) {
//                let peripheral = CBPeripheralManager(delegate: self, queue: nil)
//    let peripheralData = region.peripheralData(withMeasuredPower: nil)


//    peripheral.startAdvertising(((peripheralData as NSDictionary) as![String: Any]))
//}

//          protected void setAdvertiseData()
//          {
//              AdvertiseData.Builder mBuilder = new AdvertiseData.Builder()
//ByteBuffer mManufacturerData = ByteBuffer.allocate(24);
//              byte[] uuid = getIdAsByte(UUID.fromString("0CF052C297CA407C84F8B62AAC4E9020"));
//              mManufacturerData.put(0, (byte)0xBE); // Beacon Identifier
//              mManufacturerData.put(1, (byte)0xAC); // Beacon Identifier
//              for (int i = 2; i <= 17; i++)
//              {
//                  mManufacturerData.put(i, uuid[i - 2]); // adding the UUID
//              }
//              mManufacturerData.put(18, (byte)0x00); // first byte of Major
//              mManufacturerData.put(19, (byte)0x09); // second byte of Major
//              mManufacturerData.put(20, (byte)0x00); // first minor
//              mManufacturerData.put(21, (byte)0x06); // second minor
//              mManufacturerData.put(22, (byte)0xB5); // txPower
//              mBuilder.addManufacturerData(224, mManufacturerData.array()); // using google's company ID
//              mAdvertiseData = mBuilder.build();
//          }




/*
 *  if (this.AdvertisedBeacon != null)
        throw new ArgumentException("You are already advertising a beacon");

    var settings = new AdvertiseSettings.Builder()
        .SetAdvertiseMode(AdvertiseMode.Balanced)
        .SetConnectable(false);

    var adData = new AdvertiseData.Builder()
        .AddManufacturerData(0x004C, beacon.ToIBeaconPacket(10)); // Apple

    this.manager
        .Adapter
        .BluetoothLeAdvertiser
        .StartAdvertising(
            settings.Build(),
            adData.Build(),
            this.adCallbacks
        );

    this.AdvertisedBeacon = beacon;
 */
