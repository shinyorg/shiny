using System;
using System.ComponentModel;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Helpers the generated code calls into. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BleHostingRuntime
{
    /// <summary>
    /// Determines whether the supplied central is currently subscribed to the characteristic.
    /// </summary>
    /// <param name="characteristic">The hosted characteristic.</param>
    /// <param name="peripheral">The central to look for.</param>
    /// <returns>True when the central is subscribed.</returns>
    public static bool IsSubscribed(IGattCharacteristic? characteristic, IPeripheral peripheral)
    {
        if (characteristic == null || peripheral == null)
            return false;

        var subscribed = characteristic.SubscribedCentrals;
        for (var i = 0; i < subscribed.Count; i++)
        {
            // platforms hand back cached instances, but compare identity too in case they don't
            if (ReferenceEquals(subscribed[i], peripheral) ||
                String.Equals(subscribed[i].Uuid, peripheral.Uuid, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }


    /// <summary>
    /// Encodes an L2CAP PSM the way the generated PSM characteristic serves it - two little-endian bytes.
    /// </summary>
    /// <param name="psm">The assigned PSM, or zero when the listener is not open yet.</param>
    /// <returns>The two byte payload.</returns>
    public static byte[] EncodePsm(ushort psm)
        => new[] { (byte)(psm & 0xFF), (byte)((psm >> 8) & 0xFF) };
}
