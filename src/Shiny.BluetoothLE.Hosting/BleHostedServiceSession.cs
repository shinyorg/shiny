using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Everything the generated <c>AttachBleHostedServices</c> call brought up. Dispose to remove the
/// GATT services and close the L2CAP listeners.
/// </summary>
public sealed class BleHostedServiceSession : IAsyncDisposable
{
    readonly IBleHostingManager manager;
    readonly List<string> serviceUuids;
    readonly List<L2CapInstance> l2CapInstances;
    readonly List<Action> shutdowns;
    bool disposed;


    /// <summary>
    /// Creates a new instance. Called by generated code.
    /// </summary>
    /// <param name="manager">The hosting manager the services were added to.</param>
    /// <param name="services">The GATT services that were registered.</param>
    /// <param name="l2CapInstances">The L2CAP listeners that were opened.</param>
    /// <param name="shutdowns">Per-service teardown callbacks that cancel in-flight handlers.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public BleHostedServiceSession(
        IBleHostingManager manager,
        IReadOnlyList<IGattService> services,
        IReadOnlyList<L2CapInstance> l2CapInstances,
        IReadOnlyList<Action> shutdowns
    )
    {
        this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        this.Services = services ?? throw new ArgumentNullException(nameof(services));
        this.l2CapInstances = new List<L2CapInstance>(l2CapInstances ?? throw new ArgumentNullException(nameof(l2CapInstances)));
        this.shutdowns = new List<Action>(shutdowns ?? throw new ArgumentNullException(nameof(shutdowns)));

        this.serviceUuids = new List<string>(this.Services.Count);
        foreach (var service in this.Services)
            this.serviceUuids.Add(service.Uuid);
    }


    /// <summary>
    /// Gets the GATT services that were registered, one per distinct service UUID.
    /// </summary>
    public IReadOnlyList<IGattService> Services { get; }

    /// <summary>
    /// Gets the PSMs assigned to the L2CAP listeners that were opened.
    /// </summary>
    public IReadOnlyList<ushort> Psms
    {
        get
        {
            var result = new ushort[this.l2CapInstances.Count];
            for (var i = 0; i < this.l2CapInstances.Count; i++)
                result[i] = this.l2CapInstances[i].Psm;

            return result;
        }
    }


    /// <summary>
    /// Removes the registered services and closes the L2CAP listeners.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (this.disposed)
            return default;

        this.disposed = true;

        foreach (var shutdown in this.shutdowns)
        {
            try { shutdown(); } catch { /* best effort */ }
        }
        this.shutdowns.Clear();

        foreach (var instance in this.l2CapInstances)
        {
            // one bad listener must not strand the rest
            try { instance.Dispose(); } catch { /* best effort */ }
        }
        this.l2CapInstances.Clear();

        foreach (var uuid in this.serviceUuids)
        {
            try { this.manager.RemoveService(uuid); } catch { /* best effort */ }
        }
        this.serviceUuids.Clear();

        return default;
    }
}
