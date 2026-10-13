using System;
using CoreFoundation;

namespace Shiny.BluetoothLE;


public record AppleBleConfiguration(
    /// <summary>
    /// This will display an alert dialog when the user powers off their bluetooth adapter
    /// </summary>
    bool ShowPowerAlert = false,

    /// <summary>
    /// Dispatch queue for CBCentralManager to use - leave null if you do not know what this does
    /// </summary>
    DispatchQueue? DispatchQueue = null
)
{
    /// <summary>
    /// How long a write without response waits for CoreBluetooth to accept it (<c>canSendWriteWithoutResponse</c>)
    /// before failing with a <see cref="BleException"/>. Default 10 seconds.
    /// </summary>
    /// <remarks>
    /// The wait holds the peripheral operation queue, so while it lasts no other GATT operation runs. A peripheral
    /// that stops draining its buffer would otherwise hold it until the link drops. Use
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait for as long as the connection is up.
    /// </remarks>
    public TimeSpan WriteWithoutResponseTimeout { get; init; } = TimeSpan.FromSeconds(10);
}
