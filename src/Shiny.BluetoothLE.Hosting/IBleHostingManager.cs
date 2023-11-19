using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


public interface IBleHostingManager
{
    Task<AccessState> RequestAccess(bool advertise = true, bool connect = true);
    AccessState AdvertisingAccessStatus { get; }
    AccessState GattAccessStatus { get; }

    bool IsAdvertising { get; }
    Task StartAdvertising(AdvertisementOptions? options = null);
    void StopAdvertising();

    //Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen);
    Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null);

    Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
    void RemoveService(string serviceUuid);
    void ClearServices();

    bool IsRegisteredServicesAttached { get; }
    Task AttachRegisteredServices();
    void DetachRegisteredServices();

    IReadOnlyList<IGattService> Services { get; }
}
