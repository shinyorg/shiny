using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Net.Vpn.NetworkManager;
using Xunit;

namespace Shiny.Net.Vpn.Tests;


/// <summary>
/// A NetworkManager match rule dies with its D-Bus connection. These pin down that the Linux manager
/// adds its watches again after the bus drops, backs off while it cannot, and stops when nobody listens.
/// </summary>
public class LinuxVpnResubscribeTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);


    [Fact]
    public async Task ALostConnectionIsWatchedAgainAndTheStateReRead()
    {
        var source = new FakeWatchSource();
        await using var vpn = new TestLinuxVpnManager(source);
        var seen = new List<VpnConnectionState>();
        EventHandler<VpnConnectionState> handler = (_, state) => { lock (seen) seen.Add(state); };

        vpn.StatusChanged += handler;
        await WaitFor(() => source.Attempts == 1 && Count(seen) == 1);
        var first = source.Watches[0];

        // the tunnel comes up while the bus is down, and the bus stays down for two attempts
        vpn.State = VpnConnectionState.Connected;
        source.FailNext = 2;
        first.Drop();

        await WaitFor(() => source.Watches.Count == 2 && Count(seen) == 2);
        Assert.Equal(4, source.Attempts);
        Assert.True(first.Disposed);
        lock (seen)
            Assert.Equal([VpnConnectionState.Disconnected, VpnConnectionState.Connected], seen);

        vpn.StatusChanged -= handler;
        await WaitFor(() => source.Watches[1].Disposed);
    }


    [Fact]
    public async Task NoLongerListeningStopsTheBackoff()
    {
        var source = new FakeWatchSource { FailNext = int.MaxValue };
        await using var vpn = new TestLinuxVpnManager(source);
        EventHandler<VpnConnectionState> handler = (_, _) => { };

        vpn.StatusChanged += handler;
        await WaitFor(() => source.Attempts >= 2);
        vpn.StatusChanged -= handler;

        await Task.Delay(100);
        var attempts = source.Attempts;
        await Task.Delay(200);
        Assert.Equal(attempts, source.Attempts);
    }


    [Fact]
    public async Task NoLongerListeningDisposesTheLiveWatch()
    {
        var source = new FakeWatchSource();
        await using var vpn = new TestLinuxVpnManager(source);
        EventHandler<VpnConnectionState> handler = (_, _) => { };

        vpn.StatusChanged += handler;
        await WaitFor(() => source.Watches.Count == 1);
        vpn.StatusChanged -= handler;

        await WaitFor(() => source.Watches[0].Disposed);
        await Task.Delay(100);
        Assert.Equal(1, source.Attempts);
    }


    static int Count(List<VpnConnectionState> seen)
    {
        lock (seen)
            return seen.Count;
    }


    static async Task WaitFor(Func<bool> condition)
    {
        var until = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < until, "Timed out waiting for the condition");
            await Task.Delay(5);
        }
    }


    sealed class TestLinuxVpnManager : LinuxVpnManager
    {
        public TestLinuxVpnManager(INmVpnWatchSource source) : base(NullLogger<LinuxVpnManager>.Instance, source)
        {
            this.ResubscribeDelay = TimeSpan.FromMilliseconds(10);
            this.MaxResubscribeDelay = TimeSpan.FromMilliseconds(40);
        }

        public volatile VpnConnectionState State = VpnConnectionState.Disconnected;

        protected override Task<VpnConnectionState> GetStatusCore(CancellationToken ct)
            => Task.FromResult(this.State);
    }


    sealed class FakeWatch : IDisposable
    {
        readonly TaskCompletionSource<Exception?> lost = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public FakeWatch() => this.Watch = new NmWatch([this], this.lost.Task);

        public NmWatch Watch { get; }
        public volatile bool Disposed;

        public void Drop() => this.lost.TrySetResult(null);
        public void Dispose() => this.Disposed = true;
    }


    sealed class FakeWatchSource : INmVpnWatchSource
    {
        int attempts;
        public int Attempts => Volatile.Read(ref this.attempts);
        public volatile int FailNext;
        public List<FakeWatch> Watches { get; } = new();

        public Task<NmWatch> Watch(Action onPropertiesChanged, Action<NmVpnStateChange> onVpnStateChanged, CancellationToken ct = default)
        {
            Interlocked.Increment(ref this.attempts);
            if (this.FailNext > 0)
            {
                this.FailNext--;
                return Task.FromException<NmWatch>(new InvalidOperationException("No system bus"));
            }
            var watch = new FakeWatch();
            lock (this.Watches)
                this.Watches.Add(watch);

            return Task.FromResult(watch.Watch);
        }
    }
}
