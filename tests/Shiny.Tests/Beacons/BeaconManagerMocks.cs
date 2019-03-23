//using System;
//using System.Reactive.Subjects;
//using Plugin.BluetoothLE;
//using Moq;


//namespace Acr.Tests.Beacons
//{
//    public class BeaconManagerMocks
//    {
//        readonly Subject<IScanResult> scanSubject;


//        public BeaconManagerMocks()
//        {
//            this.scanSubject = new Subject<IScanResult>();
//            this.BleAdapter = new Mock<IAdapter>();
//            this.BleAdapter.Setup(x => x.Scan()).Returns(this.scanSubject);
//            this.Manager = new BeaconManagerImpl(new BeaconSettings(), this.BleAdapter.Object);
//        }


//        public Mock<IAdapter> BleAdapter { get; }
//        public IBeaconManager Manager { get; }


//        public void SendRange(Beacon beacon, IPeripheral peripheral = null)
//        {
//            if (peripheral == null)
//            {
//                var dev = new Mock<IPeripheral>();
//                dev.Setup(x => x.Uuid).Returns(Guid.NewGuid());
//                dev.Setup(x => x.Identifier).Returns("test");
//                peripheral = dev.Object;
//            }

//            var bytes = beacon.ToIBeaconPacket();
//            var ad = new Mock<IAdvertisementData>();
//            ad.Setup(x => x.ManufacturerData).Returns(bytes);
//            var scanResult = new ScanResult(peripheral, 0, ad.Object);
//            this.scanSubject.OnNext(scanResult);
//        }
//    }
//}
