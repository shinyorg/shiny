using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Manages BLE peripheral hosting including advertising and GATT server services
/// </summary>
public interface IBleHostingManager
{
    /// <summary>
    /// Requests access for BLE hosting operations
    /// </summary>
    /// <param name="advertise">Whether to request advertising access</param>
    /// <param name="connect">Whether to request GATT connection access</param>
    /// <returns>The resulting access state</returns>
    Task<AccessState> RequestAccess(bool advertise = true, bool connect = true);

    /// <summary>
    /// Gets the current advertising access state
    /// </summary>
    AccessState AdvertisingAccessStatus { get; }

    /// <summary>
    /// Gets the current GATT server access state
    /// </summary>
    AccessState GattAccessStatus { get; }

    /// <summary>
    /// Gets whether the device is currently advertising
    /// </summary>
    bool IsAdvertising { get; }

    /// <summary>
    /// Starts BLE advertising
    /// </summary>
    /// <param name="options">Optional advertisement configuration</param>
    Task StartAdvertising(AdvertisementOptions? options = null);

    /// <summary>
    /// Stops BLE advertising
    /// </summary>
    void StopAdvertising();

    //Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen);

    /// <summary>
    /// Advertises as an iBeacon
    /// </summary>
    /// <param name="uuid">The beacon proximity UUID</param>
    /// <param name="major">The beacon major value</param>
    /// <param name="minor">The beacon minor value</param>
    /// <param name="txpower">Optional transmit power</param>
    Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null);

    /// <summary>
    /// Adds a GATT service to the local GATT server
    /// </summary>
    /// <param name="uuid">The service UUID</param>
    /// <param name="primary">Whether this is a primary service</param>
    /// <param name="serviceBuilder">Action to configure the service characteristics</param>
    /// <returns>The created GATT service</returns>
    Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);

    /// <summary>
    /// Removes a GATT service by UUID
    /// </summary>
    /// <param name="serviceUuid">The UUID of the service to remove</param>
    void RemoveService(string serviceUuid);

    /// <summary>
    /// Removes all GATT services from the server
    /// </summary>
    void ClearServices();

    /// <summary>
    /// Gets whether DI-registered GATT services are currently attached
    /// </summary>
    bool IsRegisteredServicesAttached { get; }

    /// <summary>
    /// Attaches all GATT services registered via dependency injection
    /// </summary>
    Task AttachRegisteredServices();

    /// <summary>
    /// Detaches all DI-registered GATT services
    /// </summary>
    void DetachRegisteredServices();

    /// <summary>
    /// Gets the list of active GATT services
    /// </summary>
    IReadOnlyList<IGattService> Services { get; }
}
