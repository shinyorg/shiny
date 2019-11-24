using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Central
{
    public static class PeripheralExtensions
    {
        public static bool IsConnected(this IPeripheral peripheral) => peripheral.Status == ConnectionState.Connected;
        public static bool IsDisconnected(this IPeripheral peripheral) => !peripheral.IsConnected();


        /// <summary>
        /// Starts connection process if not already connecteds
        /// </summary>
        /// <param name="peripheral"></param>
        public static void ConnectIf(this IPeripheral peripheral,ConnectionConfig connectionConfig=null)
        {
            if (peripheral.Status == ConnectionState.Disconnected)
                peripheral.Connect(connectionConfig);
        }


        /// <summary>
        /// Continuously reads RSSI from a connected peripheral
        /// WARNING: you really don't want to run this with an Android GATT connection
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="readInterval"></param>
        /// <returns></returns>
        public static IObservable<int> ReadRssiContinuously(this IPeripheral peripheral, TimeSpan? readInterval = null) => Observable
            .Interval(readInterval ?? TimeSpan.FromSeconds(1))
            .Select(_ => peripheral.ReadRssi())
            .Switch();


        /// <summary>
        /// When peripheral is connected, this will call for RSSI continuously
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="readInterval"></param>
        /// <returns></returns>
        public static IObservable<int> WhenReadRssiContinuously(this IPeripheral peripheral, TimeSpan? readInterval = null)
            => peripheral
                .WhenConnected()
                .Select(x => x.ReadRssiContinuously(readInterval))
                .Switch();


        /// <summary>
        /// Waits for connection to actually happen
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> ConnectWait(this IPeripheral peripheral)
            => Observable.Create<IPeripheral>(ob =>
            {
                var sub1 = peripheral
                    .WhenConnected()
                    .Take(1)
                    .Subscribe(_ => ob.Respond(peripheral));

                var sub2 = peripheral
                    .WhenConnectionFailed()
                    .Subscribe(ob.OnError);

                peripheral.ConnectIf();
                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                    if (peripheral.Status != ConnectionState.Connected)
                        peripheral.CancelConnection();
                };
            });


        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuids"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> Notify(this IPeripheral peripheral, Guid serviceUuid,
            params Guid[] characteristicUuids)
            => peripheral
                .WhenConnected()
                .Select(_ => peripheral.WhenKnownCharacteristicsDiscovered(serviceUuid, characteristicUuids))
                .Switch()
                .Select(x => x.RegisterAndNotify(false, false))
                .Switch();


        /// <summary>
        /// Discover the characteristic and write to it
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="withResponse"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> WriteCharacteristic(this IPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, byte[] data, bool withResponse = true)
            => peripheral
                .GetKnownCharacteristics(serviceUuid, characteristicUuid)
                .Select(x => x.Write(data, withResponse))
                .Switch();


        /// <summary>
        /// Discover the characteristic and read it
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadCharacteristic(this IPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid)
            => peripheral
                .GetKnownCharacteristics(serviceUuid, characteristicUuid)
                .Select(ch => ch.Read())
                .Switch();


        /// <summary>
        /// Discover the known characteristic and read on a set interval
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ReadIntervalCharacteristic(this IPeripheral peripheral, Guid serviceUuid, Guid characteristicUuid, TimeSpan timeSpan)
            => peripheral
                .GetKnownCharacteristics(serviceUuid, characteristicUuid)
                .Select(ch => ch.ReadInterval(timeSpan))
                .Switch();


        /// <summary>
        /// Get known characteristic(s) without service instance
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetKnownCharacteristics(this IPeripheral peripheral, Guid serviceUuid, params Guid[] characteristicIds) =>
            peripheral
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.GetKnownCharacteristics(characteristicIds));



        /// <summary>
        /// Discovers all characteristics for a known service
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> GetCharacteristicsForService(this IPeripheral peripheral, Guid serviceUuid) =>
            peripheral
                .GetKnownService(serviceUuid)
                .SelectMany(x => x.DiscoverCharacteristics());


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> WhenConnected(this IPeripheral peripheral) =>
            peripheral
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Connected)
                .Select(_ => peripheral);


        /// <summary>
        /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Disconnected)
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> WhenDisconnected(this IPeripheral peripheral) =>
            peripheral
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Disconnected)
                .Select(_ => peripheral);


        /// <summary>
        /// Will call GetKnownCharacteristics when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicIds"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenKnownCharacteristicsDiscovered(this IPeripheral peripheral, Guid serviceUuid, params Guid[] characteristicIds) =>
            peripheral
                .WhenConnected()
                .SelectMany(x => x.GetKnownCharacteristics(serviceUuid, characteristicIds));


        /// <summary>
        /// Get a known service when the peripheral is connected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <returns></returns>
        public static IObservable<IGattService> WhenConnectedGetKnownService(this IPeripheral peripheral, Guid serviceUuid) =>
            peripheral
                .WhenConnected()
                .Select(x => x.GetKnownService(serviceUuid))
                .Switch();

        /// <summary>
        /// Will discover all services/characteristics when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic> WhenAnyCharacteristicDiscovered(this IPeripheral peripheral) =>
            peripheral
                .WhenConnected()
                .Select(x => peripheral.DiscoverServices())
                .Switch()
                .SelectMany(x => x.DiscoverCharacteristics());


        /// <summary>
        /// Will discover all services/characteristics/descriptors when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IGattDescriptor> WhenAnyDescriptorDiscovered(this IPeripheral peripheral)
            => peripheral.WhenAnyCharacteristicDiscovered().SelectMany(x => x.DiscoverDescriptors());
    }
}
