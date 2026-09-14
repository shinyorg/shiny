using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using Android.Bluetooth;
using Android.Content;
using AGattStatus = Android.Bluetooth.GattStatus;

namespace Shiny.BluetoothLE.Hosting.Internals;


public class GattServerContext : BluetoothGattServerCallback
{
    public GattServerContext(AndroidPlatform platform)
    {
        this.Platform = platform;
        this.Manager = platform.GetSystemService<BluetoothManager>(Context.BluetoothService);
    }


    public AndroidPlatform Platform { get; }
    public BluetoothManager Manager { get; }


    BluetoothGattServer server;
    public BluetoothGattServer Server
    {
        get
        {
            this.server ??= this.Manager.OpenGattServer(this.Platform.AppContext, this)!;
            return this.server!;
        }
    }


    public void CloseServer()
    {
        this.server?.Close();
        this.server = null!;
        this.ServerClosed.OnNext(Unit.Default);
    }


    /// <summary>
    /// Fires when the GATT server is closed - anything waiting on a server callback will never get one.
    /// </summary>
    public Subject<Unit> ServerClosed { get; } = new();


    readonly ConcurrentDictionary<string, SemaphoreSlim> notificationGates = new();

    /// <summary>
    /// Android allows one notification in flight per remote device across the whole server; the next has to
    /// wait for OnNotificationSent.
    /// </summary>
    public SemaphoreSlim GetNotificationGate(BluetoothDevice device)
        => this.notificationGates.GetOrAdd(device.Address!, _ => new SemaphoreSlim(1, 1));


    public Subject<CharacteristicReadEventArgs> CharacteristicRead { get; } = new();
    public override void OnCharacteristicReadRequest(BluetoothDevice device,
                                                     int requestId,
                                                     int offset,
                                                     BluetoothGattCharacteristic characteristic)
        => this.CharacteristicRead.OnNext(new CharacteristicReadEventArgs(device, characteristic, requestId, offset));

    public Subject<CharacteristicWriteEventArgs> CharacteristicWrite { get; } = new();
    public override void OnCharacteristicWriteRequest(BluetoothDevice device,
                                                      int requestId,
                                                      BluetoothGattCharacteristic characteristic,
                                                      bool preparedWrite,
                                                      bool responseNeeded,
                                                      int offset,
                                                      byte[] value)
        => this.CharacteristicWrite.OnNext(new CharacteristicWriteEventArgs(characteristic, device, requestId, offset, preparedWrite, responseNeeded, value));


    public Subject<DescriptorReadEventArgs> DescriptorRead { get; } = new();
    public override void OnDescriptorReadRequest(BluetoothDevice device, int requestId, int offset, BluetoothGattDescriptor descriptor)
        => this.DescriptorRead.OnNext(new DescriptorReadEventArgs(descriptor, device, requestId, offset));


    public Subject<DescriptorWriteEventArgs> DescriptorWrite { get; } = new();
    public override void OnDescriptorWriteRequest(BluetoothDevice device,
                                                  int requestId,
                                                  BluetoothGattDescriptor descriptor,
                                                  bool preparedWrite,
                                                  bool responseNeeded,
                                                  int offset,
                                                  byte[] value)
        => this.DescriptorWrite.OnNext(new DescriptorWriteEventArgs(descriptor, device, requestId, offset, preparedWrite, responseNeeded, value));


    public Subject<ConnectionStateChangeEventArgs> ConnectionStateChanged { get; } = new();
    public override void OnConnectionStateChange(BluetoothDevice device, ProfileState status, ProfileState newState)
        => this.ConnectionStateChanged.OnNext(new ConnectionStateChangeEventArgs(device, status, newState));


    public Subject<NotificationSentEventArgs> NotificationSent { get; } = new();
    public override void OnNotificationSent(BluetoothDevice peripheral, AGattStatus status)
        => this.NotificationSent.OnNext(new NotificationSentEventArgs(peripheral, status));


    public Subject<MtuChangedEventArgs> MtuChanged { get; } = new();
    public override void OnMtuChanged(BluetoothDevice peripheral, int mtu)
    {
        base.OnMtuChanged(peripheral, mtu);
        this.MtuChanged.OnNext(new MtuChangedEventArgs(peripheral, mtu));
    }


    //public override void OnExecuteWrite(BluetoothDevice peripheral, int requestId, bool execute)
    public Subject<(ProfileState Status, BluetoothGattService Service)> WhenServiceAdded { get; } = new();
    public override void OnServiceAdded(ProfileState status, BluetoothGattService service) => this.WhenServiceAdded.OnNext((status, service));

}
