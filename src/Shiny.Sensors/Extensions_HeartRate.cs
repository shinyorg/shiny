//using System;
//using System.Reactive.Linq;


//namespace Shiny.BluetoothLE.Central
//{
//    public static partial class Extensions
//    {
//        public static Guid HeartRateServiceUuid = new Guid("0000180d-0000-1000-8000-00805f9b34fb");


//        /// <summary>
//        /// Scan for heart rate sensors.  Note that a lot of heart rate sensors do not advertise their service UUID
//        /// </summary>
//        /// <param name="adapter"></param>
//        /// <returns></returns>
//        public static IObservable<IScanResult> ScanForHeartRateSensors(this ICentralManager centralManager) => centralManager.Scan(new ScanConfig
//        {
//            ServiceUuids =
//            {
//                HeartRateServiceUuid
//            }
//        });


//        public static IObservable<ushort> WhenHeartRateBpm(this IPeripheral peripheral) => peripheral
//            .WhenConnectedGetKnownService(HeartRateServiceUuid)
//            .Select(x => x.DiscoverCharacteristics())
//            .Switch()
//            .Take(1)
//            .Select(x => x.ReadInterval(TimeSpan.FromSeconds(3)))
//            .Switch()
//            .Select(x =>
//            {
//                if ((x.Data[0] & 0x01) == 0)
//                    return (ushort)x.Data[1];

//                var bpm = (ushort)x.Data[1];
//                bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
//                return bpm;
//            });
//    }
//}
