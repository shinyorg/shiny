using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Shiny.BluetoothLE.Hosting.Linux.Tests;


public class GattCharacteristicNotifyTests
{
    static readonly Peripheral CentralA = new("/org/bluez/hci0/dev_AA_AA_AA_AA_AA_AA", "AA:AA:AA:AA:AA:AA");
    static readonly Peripheral CentralB = new("/org/bluez/hci0/dev_BB_BB_BB_BB_BB_BB", "BB:BB:BB:BB:BB:BB");


    [Fact]
    public async Task Notify_ThrowsBeforeRegistration()
    {
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetNotification();

        await Assert.ThrowsAsync<InvalidOperationException>(() => characteristic.Notify([1]));
    }


    [Fact]
    public async Task Notify_SendsNothingWhileNobodyIsSubscribed()
    {
        var (characteristic, sent) = Registered();

        await characteristic.Notify([1]);

        Assert.Empty(sent);
    }


    [Fact]
    public async Task Notify_DispatchesWhileNotifying()
    {
        var (characteristic, sent) = Registered();
        characteristic.SetNotifying(true, [CentralA]);

        await characteristic.Notify([1, 2, 3]);

        var value = Assert.Single(sent);
        Assert.Equal(new byte[] { 1, 2, 3 }, value);
    }


    [Fact]
    public async Task Notify_SkipsWhenNoNamedCentralIsSubscribed()
    {
        var (characteristic, sent) = Registered();
        characteristic.SetNotifying(true, [CentralA]);

        await characteristic.Notify([1], CentralB);

        Assert.Empty(sent);
    }


    [Fact]
    public async Task Notify_SendsWhenANamedCentralIsSubscribed()
    {
        var (characteristic, sent) = Registered();
        characteristic.SetNotifying(true, [CentralA]);

        await characteristic.Notify([1], CentralA);

        Assert.Single(sent);
    }


    [Fact]
    public async Task Notify_HonoursACancelledToken()
    {
        var (characteristic, sent) = Registered();
        characteristic.SetNotifying(true, [CentralA]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => characteristic.Notify([1], new CancellationToken(true))
        );
        Assert.Empty(sent);
    }


    [Fact]
    public void SubscriptionState_FollowsNotifyingAndConnections()
    {
        var events = new List<(string Uuid, bool Subscribing)>();
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetNotification(x =>
        {
            events.Add((x.Peripheral.Uuid, x.IsSubscribing));
            return Task.CompletedTask;
        });

        characteristic.SetNotifying(true, [CentralA]);
        Assert.Single(characteristic.SubscribedCentrals);

        // a central connecting while notifications are on is counted - BlueZ cannot say it did not subscribe
        characteristic.OnDeviceConnectionChanged(CentralB, true);
        Assert.Equal(2, characteristic.SubscribedCentrals.Count);

        characteristic.OnDeviceConnectionChanged(CentralA, false);
        Assert.Single(characteristic.SubscribedCentrals);

        characteristic.SetNotifying(false, []);
        Assert.Empty(characteristic.SubscribedCentrals);

        Assert.Equal(
            [(CentralA.Uuid, true), (CentralB.Uuid, true), (CentralA.Uuid, false), (CentralB.Uuid, false)],
            events
        );
    }


    [Fact]
    public void Connections_AreIgnoredWhileNotificationsAreOff()
    {
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetNotification();

        characteristic.OnDeviceConnectionChanged(CentralA, true);

        Assert.Empty(characteristic.SubscribedCentrals);
    }


    static (GattCharacteristic Characteristic, List<byte[]> Sent) Registered()
    {
        var sent = new List<byte[]>();
        var characteristic = new GattCharacteristic("2A37");
        characteristic.SetNotification();
        characteristic.NotifyDispatcher = sent.Add;
        return (characteristic, sent);
    }
}
