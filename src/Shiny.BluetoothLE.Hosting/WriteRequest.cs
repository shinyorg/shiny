using System;

namespace Shiny.BluetoothLE.Hosting;

/// <summary>
/// A write request issued by a remote central against a hosted characteristic.
/// </summary>
/// <param name="Characteristic">The characteristic being written.</param>
/// <param name="Peripheral">The central performing the write.</param>
/// <param name="Data">The bytes being written.</param>
/// <param name="Offset">The byte offset for partial/long writes.</param>
/// <param name="IsReplyNeeded">True when the central used write-with-response and the host must reply via <see cref="Respond"/>.</param>
/// <param name="Respond">Callback used to send the GATT status back to the central when a reply is required.</param>
public record WriteRequest(
    IGattCharacteristic Characteristic,
    IPeripheral Peripheral,
    byte[] Data,
    int Offset,
    bool IsReplyNeeded,
    Action<GattState> Respond
);
