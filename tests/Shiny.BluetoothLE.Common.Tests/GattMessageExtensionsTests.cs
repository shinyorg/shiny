using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.BluetoothLE.Hosting;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class GattMessageExtensionsTests
{
    [Fact]
    public async Task NotifyMessage_SplitsForTheCentralsMtu_AndReassemblesToTheOriginal()
    {
        var central = new FakeCentral("central-1", mtu: 20);
        var characteristic = new RecordingCharacteristic();
        var message = new byte[1000];
        Random.Shared.NextBytes(message);

        await characteristic.NotifyMessage(message, central);

        Assert.True(characteristic.Sent.Count > 1);
        Assert.All(characteristic.Sent, x =>
        {
            Assert.True(x.Data.Length <= central.Mtu);
            Assert.Same(central, x.Central);
        });

        var reassembler = new BleMessageReassembler();
        byte[]? result = null;
        foreach (var (data, _) in characteristic.Sent)
            reassembler.Push(data, out result);

        Assert.Equal(message, result);
    }


    [Fact]
    public async Task NotifyMessage_StopsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        var characteristic = new RecordingCharacteristic(onNotify: () => cts.Cancel());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => characteristic.NotifyMessage(new byte[500], new FakeCentral("c", 20), cts.Token)
        );
        Assert.Single(characteristic.Sent);
    }


    [Fact]
    public void GetMessageReassembler_IsOnePerCentralAndCharacteristic()
    {
        var alpha = new TestContext(new FakeCentral("alpha", 185));
        var beta = new TestContext(new FakeCentral("beta", 185));

        var first = alpha.GetMessageReassembler("2a3b");
        Assert.Same(first, alpha.GetMessageReassembler("2A3B"));
        Assert.NotSame(first, alpha.GetMessageReassembler("2A3C"));
        Assert.NotSame(first, beta.GetMessageReassembler("2A3B"));
    }


    sealed class TestContext(IPeripheral peripheral) : BleServiceContext(peripheral, "180D", () => null);


    sealed class FakeCentral(string uuid, int mtu) : IPeripheral
    {
        public string Uuid => uuid;
        public int Mtu => mtu;
        public object? Context { get; set; }
    }


    sealed class RecordingCharacteristic(Action? onNotify = null) : IGattCharacteristic
    {
        public List<(byte[] Data, IPeripheral Central)> Sent { get; } = new();
        public string Uuid => "2A3B";
        public CharacteristicProperties Properties => CharacteristicProperties.Notify;
        public IReadOnlyList<IPeripheral> SubscribedCentrals => Array.Empty<IPeripheral>();

        public Task Notify(byte[] data, params IPeripheral[] centrals) => this.Notify(data, CancellationToken.None, centrals);

        public Task Notify(byte[] data, CancellationToken cancellationToken, params IPeripheral[] centrals)
        {
            this.Sent.Add((data, centrals[0]));
            onNotify?.Invoke();
            return Task.CompletedTask;
        }
    }
}
