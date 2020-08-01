using System;
using System.Text;


namespace Shiny.BluetoothLE.RefitClient.Infrastructure
{
    class DefaultBleSerializer : IBleDataSerializer
    {
        public static DefaultBleSerializer Instance { get; } = new DefaultBleSerializer();


        public object Deserialize(Type objectType, byte[] data)
        {
            if (objectType == typeof(bool))
                return BitConverter.ToBoolean(data, 0);

            if (objectType == typeof(short))
                return BitConverter.ToInt16(data, 0);

            if (objectType == typeof(ushort))
                return BitConverter.ToUInt16(data, 0);

            if (objectType == typeof(int))
                return BitConverter.ToInt32(data, 0);

            if (objectType == typeof(uint))
                return BitConverter.ToUInt32(data, 0);

            if (objectType == typeof(long))
                return BitConverter.ToInt64(data, 0);

            if (objectType == typeof(ulong))
                return BitConverter.ToUInt64(data, 0);

            if (objectType == typeof(DateTimeOffset))
            {
                var epoch = BitConverter.ToInt64(data, 0);
                return DateTimeOffset.FromUnixTimeSeconds(epoch);
            }

            if (objectType == typeof(string))
                return Encoding.UTF8.GetString(data);

            throw new ArgumentException("Invalid Data Type for BLE Serialization");
        }


        public byte[] Serialize(object obj)
        {
            if (obj is bool bvalue)
                return BitConverter.GetBytes(bvalue);

            if (obj is short svalue)
                return BitConverter.GetBytes(svalue);

            if (obj is short usvalue)
                return BitConverter.GetBytes(usvalue);

            if (obj is int ivalue)
                return BitConverter.GetBytes(ivalue);

            if (obj is uint uivalue)
                return BitConverter.GetBytes(uivalue);

            if (obj is long lvalue)
                return BitConverter.GetBytes(lvalue);

            if (obj is ulong ulvalue)
                return BitConverter.GetBytes(ulvalue);

            if (obj is string strvalue)
                return Encoding.UTF8.GetBytes(strvalue);

            if (obj is DateTimeOffset dt)
                return BitConverter.GetBytes(dt.ToUnixTimeSeconds());

            throw new ArgumentException("Invalid Data Type for BLE Serialization");
        }
    }
}
