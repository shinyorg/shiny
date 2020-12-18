using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public class ConnectHookArgs
    {
        public ConnectHookArgs(string serviceUuid, params string[] characteristicUuids)
            : this(null, serviceUuid, characteristicUuids) {}


        public ConnectHookArgs(ConnectionConfig? config, string serviceUuid, params string[] characteristicUuids)
        {
            this.Config = config;
            this.ServiceUuid = serviceUuid;
            this.CharacteristicUuids = characteristicUuids;
        }


        public ConnectionConfig? Config { get; set; }
        public string ServiceUuid { get; }
        public string[] CharacteristicUuids { get; }
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
        public static IObservable<CharacteristicGattResult> ConnectHook(this IPeripheral peripheral, string serviceUuid, params string[] characteristicUuids) => peripheral.ConnectHook(new ConnectHookArgs(serviceUuid, characteristicUuids));


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
