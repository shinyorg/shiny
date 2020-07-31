using System;


namespace Shiny.BluetoothLE.RefitClient.Infrastructure
{
    internal class DefaultBleSerializer : IBleDataSerializer
    {
        public static DefaultBleSerializer Instance { get; } = new DefaultBleSerializer();


        public object Deserialize(Type objectType, byte[] data)
        {
            if (objectType == typeof(bool))
                return BitConverter.ToBoolean(data, 0);

            if (objectType == typeof(int))
                return BitConverter.ToInt32(data, 0);

            throw new ArgumentException("");
        }


        public byte[] Serialize(object obj)
        {
            if (obj is int ivalue)
                return BitConverter.GetBytes(ivalue);

            if (obj is bool bvalue)
                return BitConverter.GetBytes(bvalue);

            throw new ArgumentException("");
        }
    }
}
