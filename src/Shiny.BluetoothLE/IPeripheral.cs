using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public record BleServiceInfo(string Uuid);

public record BleCharacteristicInfo(
    BleServiceInfo Service,
    string Uuid,
    bool IsNotifying,
    CharacteristicProperties Properties
);
public record BleDescriptorInfo(
    BleCharacteristicInfo Characteristic,
    string Uuid
);
public record BleCharacteristicResult(
    BleCharacteristicInfo Characteristic,
    BleCharacteristicEvent Event,
    byte[]? Data
);
public enum BleCharacteristicEvent
{
    Read,
    Write,
    WriteWithoutResponse,
    Notification,
    NotificationSubscribed
}

public record BleDescriptorResult(
    BleDescriptorInfo Descriptor,
    byte[]? Data
);

public interface IPeripheral
{
    string Uuid { get; }
    string? Name { get; }
    int Mtu { get; }
    ConnectionState Status { get; } // TODO: add failed state?

    /// <summary>
    /// Sets up an asynchronous connection request
    /// </summary>
    /// <param name="config">The connection configuration</param>
    void Connect(ConnectionConfig? config);

    /// <summary>
    /// Cancel any current requested connection with this peripheral - YOU MUST make this call to remove connections
    /// </summary>
    void CancelConnection();

    /// <summary>
    /// Monitor connection status
    /// </summary>
    /// <returns></returns>
    IObservable<ConnectionState> WhenStatusChanged();
    
    /// <summary>
    /// Reads the peripheral RSSI
    /// </summary>
    /// <returns>A completed observable with the RSSI</returns>
    IObservable<int> ReadRssi();

    /// <summary>
    /// Get a known service - this will pull from cached services, if not found, will initiate a discovery
    /// </summary>
    /// <param name="serviceUuid">The UUID of the service</param>
    /// <returns>Returns a completed observable with a service or an exception if the service is not found</returns>
    IObservable<BleServiceInfo> GetService(string serviceUuid);
    
    /// <summary>
    /// Runs a discovery process for all services - will force a fresh cache rebuild of all services
    /// </summary>
    /// <returns></returns>
    IObservable<IReadOnlyList<BleServiceInfo>> GetServices();

    /// <summary>
    /// Get a characteristic - if not found, a BleException is thrown
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <returns></returns>
    IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Discover characteristics for a specific service UUID
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <returns></returns>
    IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid);
    
    /// <summary>
    /// Connects to a characteristic if not already subscribed, it will also attempt to auto-reconnect if you keep the observable hooked
    /// </summary>
    /// <param name="serviceUuid">The service UUID</param>
    /// <param name="characteristicUuid">The characteristic UUID</param>
    /// <param name="useIndicationsIfAvailable">Uses indications (acks) if available, otherwise uses standard notify</param>
    /// <param name="emitSubscribedEvent">
    /// If true, the notificaton BleCharacteristicEvent will be emitted with empty data and notification subscribed - useful for knowing when hook is made
    /// WARNING: If subscriber has multiple hooks, this event will only be emitted to the first subscriber
    /// </param>
    /// <returns></returns>
    IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true, bool emitSubscribedEvent = false);

    /// <summary>
    /// Reads a characteristic
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <returns></returns>
    IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Write to characterisitic with or without response (as long as operation is supported)
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <param name="data"></param>
    /// <param name="withResponse"></param>
    /// <returns></returns>
    IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);

    /// <summary>
    /// Gets a descriptor, exception if not found
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <param name="descriptorUuid"></param>
    /// <returns></returns>
    IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    /// <summary>
    /// Gets all descriptors for a characterisitic
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <returns></returns>
    IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Reads a descriptor
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <param name="descriptorUuid"></param>
    /// <returns></returns>
    IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    /// <summary>
    /// Writes a descriptor
    /// </summary>
    /// <param name="serviceUuid"></param>
    /// <param name="characteristicUuid"></param>
    /// <param name="descriptorUuid"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data);
}