using System;


namespace Shiny.BluetoothLE.RefitClient
{
    public interface IBleDataSerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] data);
    }
}
