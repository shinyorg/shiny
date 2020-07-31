using System;


namespace Shiny.BluetoothLE.RefitClient
{
    public interface IBleDataSerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(Type objectType, byte[] data);
    }
}
