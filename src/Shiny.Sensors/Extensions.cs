using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;


namespace Shiny.Sensors
{
    public static class Extensions
    {
        // credit to Xamarin Essentials for this calculation
        public static IObservable<Unit> WhenShakeDetected(this IAccelerometer accelerometer,
                                                          TimeSpan? bufferWindow = null,
                                                          double gravity = 9.81,
                                                          double accelerationThreshold = 169,
                                                          int minReadings = 4,
                                                          double avgNeededForShake = 0.75)
            => accelerometer
                .WhenReadingTaken()
                .Buffer(bufferWindow ?? TimeSpan.FromMilliseconds(500))
                .Select(readings =>
                {
                    if (readings.Count < minReadings)
                        return false;

                    var countInAccel = readings.Count(acc =>
                    {
                        var x = acc.X * gravity;
                        var y = acc.Y * gravity;
                        var z = acc.Z * gravity;

                        var g = (x * x) + (y * y) + (z * z);

                        return g > accelerationThreshold; // is accelerating
                    });
                    var avg = countInAccel / readings.Count;

                    return (avg >= avgNeededForShake);
                })
                .Where(x => x)
                .Select(_ => Unit.Default);


        //public static Guid HeartRateServiceUuid = new Guid("0000180d-0000-1000-8000-00805f9b34fb");


        ///// <summary>
        ///// Scan for heart rate sensors.  Note that a lot of heart rate sensors do not advertise their service UUID
        ///// </summary>
        ///// <param name="adapter"></param>
        ///// <returns></returns>
        //public static IObservable<IScanResult> ScanForHeartRateSensors(this ICentralManager centralManager) => centralManager.Scan(new ScanConfig
        //{
        //    ServiceUuids =
        //    {
        //        HeartRateServiceUuid
        //    }
        //});


        //public static IObservable<ushort> WhenHeartRateBpm(this IPeripheral peripheral) => peripheral
        //    .WhenConnectedGetKnownService(HeartRateServiceUuid)
        //    .Select(x => x.DiscoverCharacteristics())
        //    .Switch()
        //    .Take(1)
        //    .Select(x => x.ReadInterval(TimeSpan.FromSeconds(3)))
        //    .Switch()
        //    .Select(x =>
        //    {
        //        if ((x.Data[0] & 0x01) == 0)
        //            return (ushort)x.Data[1];

        //        var bpm = (ushort)x.Data[1];
        //        bpm = (ushort)(((bpm >> 8) & 0xFF) | ((bpm << 8) & 0xFF00));
        //        return bpm;
        //    });
    }
}
