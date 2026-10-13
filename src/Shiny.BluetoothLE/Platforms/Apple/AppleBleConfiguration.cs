using System;
using CoreFoundation;

namespace Shiny.BluetoothLE;


public record AppleBleConfiguration(
    /// <summary>
    /// This will display an alert dialog when the user powers off their bluetooth adapter
    /// </summary>
    bool ShowPowerAlert = false,


    /// <summary>
    /// CBCentralInitOptions restoration key for background restoration
    /// </summary>
    string? RestoreIdentifier = null,

    /// <summary>
    /// Dispatch queue for CBCentralManager to use - leave null if you do not know what this does
    /// </summary>
    DispatchQueue? DispatchQueue = null
)
{
    // Init properties rather than positional parameters: adding to the primary constructor would change its
    // signature and break binaries compiled against an earlier 5.8.x.

    /// <summary>
    /// Ask iOS to show an "accessory would like to open app" alert when a connection completes while the app
    /// is not in the foreground (<c>CBConnectPeripheralOptionNotifyOnConnectionKey</c>). Default true.
    /// </summary>
    /// <remarks>
    /// Apple intends these alerts for apps that do not declare the <c>bluetooth-central</c> background mode and
    /// so cannot raise their own. An app that connects from the background on purpose — kept running by
    /// location updates, for example — gets the alert on every such connect; turn this off there.
    /// </remarks>
    public bool NotifyOnConnection { get; init; } = true;

    /// <summary>
    /// Ask iOS to show an alert when a peripheral disconnects while the app is not in the foreground
    /// (<c>CBConnectPeripheralOptionNotifyOnDisconnectionKey</c>). Default true.
    /// </summary>
    public bool NotifyOnDisconnection { get; init; } = true;

    /// <summary>
    /// Ask iOS to show an alert for every notification a peripheral sends while the app is not in the
    /// foreground (<c>CBConnectPeripheralOptionNotifyOnNotificationKey</c>). Default true.
    /// </summary>
    /// <remarks>
    /// ⚠️ For a device that streams notifications — a serial-over-BLE bridge answering a poll, a sensor — this
    /// is an alert per packet for as long as the app stays in the background.
    /// </remarks>
    public bool NotifyOnNotification { get; init; } = true;

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
