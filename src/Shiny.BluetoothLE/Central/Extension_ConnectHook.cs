using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE.Central
{
    public class ConnectHookArgs
    {
        public ConnectHookArgs(Guid serviceUuid, params Guid[] characteristicUuids)
            : this(null, serviceUuid, characteristicUuids) {}


        public ConnectHookArgs(ConnectionConfig? config, Guid serviceUuid, params Guid[] characteristicUuids)
        {
            this.Config = config;
            this.ServiceUuid = serviceUuid;
            this.CharacteristicUuids = characteristicUuids;
        }


        public ConnectionConfig? Config { get; set; }
        public Guid ServiceUuid { get; }
        public Guid[] CharacteristicUuids { get; }
    }


    public static class Extension_ConnectHook
    {
        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="serviceUuid"></param>
        /// <param name="characteristicUuids"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ConnectHook(this IPeripheral peripheral, Guid serviceUuid, params Guid[] characteristicUuids) => peripheral.ConnectHook(new ConnectHookArgs(serviceUuid, characteristicUuids));


        /// <summary>
        /// Connect and manage connection as well as hook into your required characterisitcs with all proper cleanups necessary
        /// </summary>
        /// <param name="peripheral"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static IObservable<CharacteristicGattResult> ConnectHook(this IPeripheral peripheral, ConnectHookArgs args)
            => Observable.Create<CharacteristicGattResult>(ob =>
            {
                var sub = peripheral
                    .WhenConnected()
                    .Select(_ => peripheral.WhenKnownCharacteristicsDiscovered(args.ServiceUuid, args.CharacteristicUuids))
                    .Switch()
                    .Select(x => x.Notify(false))
                    .Merge()
                    .Subscribe(
                        ob.OnNext,
                        ob.OnError
                    );

                var connSub = peripheral
                    .WhenConnectionFailed()
                    .Subscribe(ob.OnError);

                peripheral.ConnectIf(args.Config);

                return () =>
                {
                    peripheral.CancelConnection();
                    sub?.Dispose();
                    connSub?.Dispose();
                };
            });
    }
}
