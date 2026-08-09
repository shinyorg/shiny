using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;

namespace Shiny.BluetoothLE.Common.Tests.Infrastructure;


/// <summary>
/// Stands in for a platform hosting manager so the L2CAP file server can be driven from tests.
/// Only the L2CAP surface is implemented.
/// </summary>
public class FakeBleHostingManager : IBleHostingManager
{
    readonly L2CapChannel channel;


    /// <param name="channel">The channel handed to the server when it starts listening.</param>
    public FakeBleHostingManager(L2CapChannel channel)
        => this.channel = channel;


    /// <summary>Gets whether the listening PSM has been unpublished.</summary>
    public bool IsClosed { get; private set; }


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        this.Secure = secure;
        onOpen(this.channel);
        return Task.FromResult(new L2CapInstance(0x0080, () => this.IsClosed = true));
    }


    /// <summary>Gets the secure flag the server asked for.</summary>
    public bool Secure { get; private set; }


    public Task<AccessState> RequestAccess(bool advertise = true, bool connect = true) => Task.FromResult(AccessState.Available);
    public AccessState AdvertisingAccessStatus => AccessState.Available;
    public AccessState GattAccessStatus => AccessState.Available;
    public bool IsAdvertising => false;
    public Task StartAdvertising(AdvertisementOptions? options = null) => throw new NotSupportedException();
    public void StopAdvertising() => throw new NotSupportedException();
    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null) => throw new NotSupportedException();
    public Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder) => throw new NotSupportedException();
    public void RemoveService(string serviceUuid) => throw new NotSupportedException();
    public void ClearServices() => throw new NotSupportedException();
    public IReadOnlyList<IGattService> Services => Array.Empty<IGattService>();
}
