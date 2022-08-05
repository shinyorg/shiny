using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public interface IBleHostingManager
{
    Task<AccessState> RequestAccess(bool advertise = true, bool connect = true);
    IObservable<L2CapChannel> WhenL2CapChannelOpened(bool secure); // TODO: how to tell if open?

    bool IsAdvertising { get; }
    Task StartAdvertising(AdvertisementOptions? options = null);
    void StopAdvertising();

    Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
    void RemoveService(string serviceUuid);
    void ClearServices();

    bool IsRegisteredServicesAttached { get; }
    Task AttachRegisteredServices();
    void DetachRegisteredServices();

    IReadOnlyList<IGattService> Services { get; }
}
