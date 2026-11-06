using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Shiny.BluetoothLE.McuMgr.Tests;


public class BleSmpTransportTests
{
    [Fact]
    public async Task SplitsWritesByMtu_AndReassemblesNotifications()
    {
        var peripheral = new FakePeripheral { Mtu = 20 };
        var device = new FakeSmpDevice();
        peripheral.OnFrame = async frame =>
        {
            var response = await device.SendAsync(frame, default);

            // a stale response (wrong sequence) first, then the real one split over 7 byte notifications
            var stale = (byte[])response.Clone();
            stale[6] = unchecked((byte)(stale[6] + 1));
            peripheral.Notify(stale);

            foreach (var chunk in response.Chunk(7))
                peripheral.Notify(chunk);
        };

        using var client = peripheral.CreateMcuMgrClient();
        var text = new string('x', 60);
        var echo = await client.Os.EchoAsync(text);

        Assert.Equal(text, echo);
        Assert.True(peripheral.Writes.Count > 1);
        Assert.All(peripheral.Writes, x => Assert.True(x.Length <= 20));
        Assert.All(peripheral.WriteModes, x => Assert.False(x)); // write without response
    }


    [Fact]
    public async Task Disconnect_FailsPendingRequest()
    {
        var peripheral = new FakePeripheral();
        peripheral.OnFrame = _ =>
        {
            peripheral.SetStatus(ConnectionState.Disconnected);
            return Task.CompletedTask;
        };

        using var client = peripheral.CreateMcuMgrClient();
        await Assert.ThrowsAsync<McuMgrException>(() => client.Os.EchoAsync("x"));
    }


    sealed class FakePeripheral : IPeripheral
    {
        readonly Subject<BleCharacteristicResult> notifications = new();
        readonly BehaviorSubject<ConnectionState> status = new(ConnectionState.Connected);
        readonly List<byte> frame = new();
        static readonly BleCharacteristicInfo Info = new(
            new BleServiceInfo(BleSmpTransport.ServiceUuid),
            BleSmpTransport.CharacteristicUuid,
            true,
            CharacteristicProperties.WriteWithoutResponse | CharacteristicProperties.Notify
        );

        public Func<byte[], Task>? OnFrame { get; set; }
        public List<byte[]> Writes { get; } = new();
        public List<bool> WriteModes { get; } = new();

        public string Uuid => "fake";
        public string? Name => "fake";
        public int Mtu { get; set; } = 244;
        public ConnectionState Status => this.status.Value;

        public void Notify(byte[] data) => this.notifications.OnNext(new BleCharacteristicResult(Info, BleCharacteristicEvent.Notification, data));
        public void SetStatus(ConnectionState state) => this.status.OnNext(state);

        public void Connect(ConnectionConfig? config) => this.status.OnNext(ConnectionState.Connected);
        public void CancelConnection() => this.status.OnNext(ConnectionState.Disconnected);
        public IObservable<ConnectionState> WhenStatusChanged() => this.status;
        public IObservable<BleException> WhenConnectionFailed() => Observable.Never<BleException>();
        public IObservable<Unit> WhenServicesChanged() => Observable.Never<Unit>();
        public IObservable<int> ReadRssi() => Observable.Return(-50);
        public IObservable<BleServiceInfo> GetService(string serviceUuid) => Observable.Return(Info.Service);
        public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.Return<IReadOnlyList<BleServiceInfo>>([Info.Service]);
        public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => Observable.Return(Info);
        public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => Observable.Return<IReadOnlyList<BleCharacteristicInfo>>([Info]);
        public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true) => this.notifications;
        public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid) => Observable.Return(Info);
        public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();

        public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
        {
            this.Writes.Add(data);
            this.WriteModes.Add(withResponse);
            this.frame.AddRange(data);

            var expected = SmpHeader.GetFrameLength(this.frame.ToArray());
            if (expected != null && this.frame.Count >= expected)
            {
                var complete = this.frame.ToArray();
                this.frame.Clear();
                // respond after the write completes, like a real link
                Task.Run(() => this.OnFrame?.Invoke(complete) ?? Task.CompletedTask);
            }
            return Observable.Return(new BleCharacteristicResult(Info, BleCharacteristicEvent.WriteWithoutResponse, data));
        }

        public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotSupportedException();
        public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => throw new NotSupportedException();
        public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => throw new NotSupportedException();
        public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => throw new NotSupportedException();
    }
}
