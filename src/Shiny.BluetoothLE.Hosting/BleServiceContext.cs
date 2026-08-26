using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Per-connection state for a hosted GATT service, in the spirit of SignalR's <c>Hub.Context</c>.
/// The generator derives one <c>{ServiceClass}Context</c> from this per <see cref="BleServiceAttribute"/>
/// class and creates an instance the first time a given central touches that service.
/// </summary>
/// <remarks>
/// The derived type is emitted as a <c>partial class</c>, so you can stamp whatever properties you
/// like onto it in your own half of the file and they will flow through every handler on the service.
/// </remarks>
public abstract class BleServiceContext
{
    readonly Func<IGattService?> serviceAccessor;
    Dictionary<string, object?>? items;


    /// <summary>
    /// Creates a new instance. Called by generated code.
    /// </summary>
    /// <param name="peripheral">The central this context belongs to.</param>
    /// <param name="serviceUuid">The UUID of the service that owns this context.</param>
    /// <param name="serviceAccessor">Resolves the hosted service once registration has completed.</param>
    protected BleServiceContext(IPeripheral peripheral, string serviceUuid, Func<IGattService?> serviceAccessor)
    {
        this.Peripheral = peripheral ?? throw new ArgumentNullException(nameof(peripheral));
        this.ServiceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
        this.serviceAccessor = serviceAccessor ?? throw new ArgumentNullException(nameof(serviceAccessor));
    }


    /// <summary>
    /// Gets the connected central this context belongs to.
    /// </summary>
    public IPeripheral Peripheral { get; }

    /// <summary>
    /// Gets the central's connection identifier (peripheral UUID on Apple, MAC address on Android).
    /// </summary>
    public string ConnectionId => this.Peripheral.Uuid;

    /// <summary>
    /// Gets the maximum number of bytes that fit in a single GATT operation to this central - the negotiated
    /// ATT MTU minus the 3-byte ATT header. See <see cref="IPeripheral.Mtu"/>.
    /// </summary>
    public int Mtu => this.Peripheral.Mtu;

    /// <summary>
    /// Gets the UUID of the service that owns this context.
    /// </summary>
    public string ServiceUuid { get; }

    /// <summary>
    /// Gets the hosted service, or null in the brief window between the platform accepting the
    /// service and <c>AddService</c> returning. Use <see cref="ServiceUuid"/> when you only need the identity.
    /// </summary>
    public IGattService? Service => this.serviceAccessor();

    /// <summary>
    /// Gets a loosely typed bag for anything you do not want to add as a property. Created on first access.
    /// </summary>
    public IDictionary<string, object?> Items
    {
        get
        {
            // handlers for one central are not guaranteed to be serialized by the platform, so the
            // bag itself is created under the store's lock-free first-write-wins pattern
            var current = this.items;
            if (current != null)
                return current;

            var created = new Dictionary<string, object?>(StringComparer.Ordinal);
            return System.Threading.Interlocked.CompareExchange(ref this.items, created, null) ?? created;
        }
    }
}
