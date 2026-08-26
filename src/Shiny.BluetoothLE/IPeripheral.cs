using System;
using System.Collections.Generic;
using System.Reactive;

namespace Shiny.BluetoothLE;


/// <summary>
/// Represents a remote BLE peripheral that the central can connect to, query, and exchange GATT data with.
/// </summary>
public interface IPeripheral
{
    /// <summary>
    /// Gets the platform-specific peripheral identifier.
    /// </summary>
    string Uuid { get; }

    /// <summary>
    /// Gets the advertised or cached device name, if any.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the maximum number of bytes that fit in a single GATT operation on the current link - the negotiated
    /// ATT MTU minus the 3-byte ATT header. This is the size to fragment writes to; do NOT subtract the header
    /// again. Reports 20 until a larger MTU is negotiated.
    /// </summary>
    int Mtu { get; }

    /// <summary>
    /// Gets the current GATT connection state for this peripheral.
    /// </summary>
    ConnectionState Status { get; } // TODO: add failed state?

    /// <summary>
    /// Initiates an asynchronous connection request. Subscribe to <see cref="WhenStatusChanged"/> to observe completion.
    /// </summary>
    /// <param name="config">Optional connection configuration (auto-reconnect, etc.).</param>
    void Connect(ConnectionConfig? config);

    /// <summary>
    /// Cancels any pending or active connection with this peripheral. Must be called to release platform resources.
    /// </summary>
    void CancelConnection();

    /// <summary>
    /// Returns an observable that emits whenever the peripheral's connection state changes.
    /// </summary>
    /// <returns>An observable of connection state transitions.</returns>
    IObservable<ConnectionState> WhenStatusChanged();

    /// <summary>
    /// Returns an observable that emits whenever a connection attempt fails or an active connection drops with an error.
    /// </summary>
    /// <returns>An observable of connection failure exceptions.</returns>
    IObservable<BleException> WhenConnectionFailed();

    /// <summary>
    /// Returns an observable that fires when the peripheral's GATT service list changes. Re-query services after each emission.
    /// </summary>
    /// <returns>An observable that signals service-table invalidation.</returns>
    IObservable<Unit> WhenServicesChanged();

    /// <summary>
    /// Reads the current RSSI (signal strength in dBm) of the peripheral.
    /// </summary>
    /// <returns>A single-value observable with the RSSI reading.</returns>
    IObservable<int> ReadRssi();

    /// <summary>
    /// Gets a GATT service by UUID. Returns cached metadata when available, otherwise performs discovery.
    /// </summary>
    /// <param name="serviceUuid">The service UUID to look up.</param>
    /// <returns>A single-value observable with the service info; errors if not found.</returns>
    IObservable<BleServiceInfo> GetService(string serviceUuid);

    /// <summary>
    /// Performs a fresh discovery of all GATT services on the peripheral and rebuilds the service cache.
    /// </summary>
    /// <returns>A single-value observable with the discovered service list.</returns>
    IObservable<IReadOnlyList<BleServiceInfo>> GetServices();

    /// <summary>
    /// Gets a specific characteristic by UUID under the given service. Errors if not found.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <returns>A single-value observable with the characteristic info.</returns>
    IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Discovers all characteristics belonging to the specified service.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <returns>A single-value observable with the characteristic list.</returns>
    IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid);

    /// <summary>
    /// Subscribes to a characteristic and emits each notification/indication. While the subscription is alive,
    /// the peripheral will be reconnected automatically if connection is lost.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <param name="useIndicationsIfAvailable">Use indications (acknowledged) instead of notifications when both are supported.</param>
    /// <returns>An observable that emits each notification or indication payload.</returns>
    IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true);

    /// <summary>
    /// Returns an observable that emits whenever the subscription state of a specific characteristic changes.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <returns>An observable of characteristic info with the updated IsNotifying flag.</returns>
    IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Reads the current value of a characteristic.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <returns>A single-value observable with the read result.</returns>
    IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Writes a value to a characteristic, with or without an acknowledgement.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The characteristic UUID.</param>
    /// <param name="data">The bytes to write.</param>
    /// <param name="withResponse">When true, waits for a write-with-response acknowledgement; otherwise issues a write-without-response.</param>
    /// <returns>A single-value observable that completes on write acknowledgement (or immediately for write-without-response).</returns>
    IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true);

    /// <summary>
    /// Gets a specific descriptor by UUID. Errors if not found.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The owning characteristic UUID.</param>
    /// <param name="descriptorUuid">The descriptor UUID.</param>
    /// <returns>A single-value observable with the descriptor info.</returns>
    IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    /// <summary>
    /// Gets all descriptors for the specified characteristic.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The owning characteristic UUID.</param>
    /// <returns>A single-value observable with the descriptor list.</returns>
    IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid);

    /// <summary>
    /// Reads the value of a descriptor.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The owning characteristic UUID.</param>
    /// <param name="descriptorUuid">The descriptor UUID.</param>
    /// <returns>A single-value observable with the descriptor read result.</returns>
    IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid);

    /// <summary>
    /// Writes a value to a descriptor.
    /// </summary>
    /// <param name="serviceUuid">The owning service UUID.</param>
    /// <param name="characteristicUuid">The owning characteristic UUID.</param>
    /// <param name="descriptorUuid">The descriptor UUID.</param>
    /// <param name="data">The bytes to write.</param>
    /// <returns>A single-value observable that completes on write acknowledgement.</returns>
    IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data);
}
