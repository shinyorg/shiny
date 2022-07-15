using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public interface IBleHostingManager
{
    // WHENREADY on iOS needed, BLUETOOTH_CONNECT permission on Android 12+
    Task<AccessState> RequestAccess();
    IObservable<L2CapChannel> WhenL2CapChannelOpened(bool secure); // TODO: how to tell if open?

    bool IsAdvertising { get; }
    Task StartAdvertising(AdvertisementOptions? options = null);
    void StopAdvertising();

    Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
    void RemoveService(string serviceUuid);
    void ClearServices();

    IReadOnlyList<IGattService> Services { get; }
}
