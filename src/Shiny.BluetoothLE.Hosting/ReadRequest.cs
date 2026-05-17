namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// A read request issued by a remote central against a hosted characteristic.
/// </summary>
/// <param name="Characteristic">The characteristic being read.</param>
/// <param name="Peripheral">The central performing the read.</param>
/// <param name="Offset">The byte offset for partial/long reads.</param>
public record ReadRequest(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    int Offset
);
