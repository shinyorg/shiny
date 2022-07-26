using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public interface IGattCharacteristic
{
    string Uuid { get; }
    CharacteristicProperties Properties { get; }
    Task Notify(byte[] data, params IPeripheral[] centrals);
    IReadOnlyList<IPeripheral> SubscribedCentrals { get; }
    //IGattDescriptor AddDescriptor(Guid uuid);
    //IReadOnlyList<IGattDescriptor> Descriptors { get; }
}
