using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public static partial class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="data"></param>
        /// <param name="withResponse"></param>
        /// <returns></returns>
        public static IObservable<GattCharacteristicResult> WriteCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid, true)
                .Select(x => x.Write(data, withResponse))
                .Switch();

        /// <summary>
        ///
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <returns></returns>
        public static IObservable<byte[]?> ReadCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid, true)
                .Select(x => x.Read())
                .Switch()
                .Select(x => x.Data);


        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="useIndicationIfAvailable"></param>
        /// <returns></returns>
        public static IObservable<GattCharacteristicResult> Notify(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, bool useIndicationIfAvailable = false)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid, true)
                .Select(x => x.EnableNotifications(true, useIndicationIfAvailable).Select(_ => x))
                .Switch()
                .Select(x => x.WhenNotificationReceived())
                .Switch();


        /// <summary>
        /// An easy wrapper around checking peripheral status == ConnectionState.Connected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns>Return true if connected, otherwise false</returns>
        public static bool IsConnected(this IPeripheral peripheral) => peripheral.Status == ConnectionState.Connected;


        /// <summary>
        /// An easy wrapper around checking peripheral status == ConnectionState.Disconnected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns>Return true if disconnected, otherwise false</returns>
        public static bool IsDisconnected(this IPeripheral peripheral) => !peripheral.IsConnected();


        /// <summary>
        /// Starts connection process if not already connecteds
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="connectionConfig"></param>
        /// <returns>True if connection attempt was sent, otherwise false</returns>
        public static bool ConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null)
        {
            if (peripheral.Status == ConnectionState.Disconnected)
            {
                peripheral.Connect(config);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Attempts to connect if not already connected
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static IObservable<IPeripheral> WithConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null) => Observable.Create<IPeripheral>(ob =>
        {
            if (peripheral.IsConnected())
            {
                ob.Respond(peripheral);
                return Disposable.Empty;
            }

            var sub1 = peripheral
                .WhenConnected()
                .Take(1)
                .Subscribe(_ => ob.Respond(peripheral));

            var sub2 = peripheral
                .WhenConnectionFailed()
                .Subscribe(ob.OnError);

            peripheral.Connect(config);

            return Disposable.Create(() =>
            {
                sub1.Dispose();
                sub2.Dispose();
                if (peripheral.Status != ConnectionState.Connected)
                    peripheral.CancelConnection();
            });
        });


        /// <summary>
        /// Continuously reads RSSI from a connected peripheral
        /// WARNING: you really don't want to run this with an Android GATT connection
        /// </summary>
        /// <param name="peripheral">The peripheral to read RSSI</param>
        /// <param name="readInterval">The interval to poll the RSSI - defaults to 1 second if not set</param>
        /// <returns>RSSI value every read interval</returns>
        public static IObservable<int> ReadRssiContinuously(this IPeripheral peripheral, TimeSpan? readInterval = null) => Observable
            .Interval(readInterval ?? TimeSpan.FromSeconds(1))
            .Where(_ => peripheral.IsConnected())
            .Select(_ => peripheral.ReadRssi())
            .Switch();


        /// <summary>
        /// Get known characteristic(s) without service instance
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuid"></param>
        /// <param name="throwIfNotFound"></param>
        /// <returns></returns>
        public static IObservable<IGattCharacteristic?> GetKnownCharacteristic(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, bool throwIfNotFound = false) =>
            peripheral
                .GetKnownService(serviceUuid, throwIfNotFound)
                .Select(x =>
                {
                    if (x == null)
                        return Observable.Empty<IGattCharacteristic>();

                    return x.GetKnownCharacteristic(characteristicUuid, throwIfNotFound);
                })
                .Switch();


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
        /// Will discover all services/characteristics when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IList<IGattCharacteristic>> GetAllCharacteristics(this IPeripheral peripheral) =>
            peripheral
                .GetServices()
                .SelectMany(x => x.Select(y => y.GetCharacteristics()))
                .Merge();


        /// <summary>
        /// Will discover all services/characteristics/descriptors when connected state occurs
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        public static IObservable<IList<IGattDescriptor>> GetAllDescriptors(this IPeripheral peripheral) =>
            peripheral
                .GetAllCharacteristics()
                .SelectMany(x => x.Select(y => y.GetDescriptors()))
                .Switch();
    }
}
