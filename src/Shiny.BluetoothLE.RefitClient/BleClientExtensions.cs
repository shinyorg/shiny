using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Shiny.BluetoothLE.RefitClient.Infrastructure;


namespace Shiny.BluetoothLE.RefitClient
{
    public static class BleClientExtensions
    {
        public static T GetClient<T>(this IPeripheral peripheral) where T : IBleClient
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("T argument must be an interface");

            var className = typeof(T).Name.TrimStart('I');
            var typeName = $"{typeof(T).Namespace}.{className}, {typeof(T).Assembly.GetName().Name}";
            var actualType = Type.GetType(typeName);
            if (actualType == null)
                throw new ArgumentException("Generated type was not found");

            var instance = (T)Activator.CreateInstance(actualType);
            var genClient = instance as BleClient;
            if (genClient == null)
                throw new ArgumentException("This does not appear to be a generated BLE client");

            genClient.Peripheral = peripheral;
            genClient.Serializer = ShinyHost.Resolve<IBleDataSerializer>() ?? DefaultBleSerializer.Instance;
            return instance;
        }


        public static IObservable<T> ScanForClientPeripherals<T>(this IBleManager bleManager, Guid serviceUuid) where T : IBleClient
            => bleManager
                .ScanForUniquePeripherals(new ScanConfig
                {
                    ServiceUuids = new List<Guid> { serviceUuid }
                })
                .Select(x => x.GetClient<T>());
    }
}
