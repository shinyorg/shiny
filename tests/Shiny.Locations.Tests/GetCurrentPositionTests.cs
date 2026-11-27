using Xunit;

namespace Shiny.Locations.Tests;


public class GetCurrentPositionTests
{
    [Fact]
    public async Task ReturnsTheNextReading_AndStopsTheListenerItStarted()
    {
        var gps = new FakeGpsManager();
        var task = gps.GetCurrentPosition();

        await WaitFor(() => gps.CurrentListener != null);
        var reading = Reading();
        gps.Raise(reading);

        Assert.Same(reading, await task);
        Assert.Null(gps.CurrentListener);
        Assert.Equal(0, gps.HandlerCount);
    }


    [Fact]
    public async Task StartListenerThrowing_DetachesTheHandler()
    {
        // the handler used to be left on the manager for good when StartListener threw
        var gps = new FakeGpsManager { StartException = new InvalidOperationException("denied") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => gps.GetCurrentPosition());

        Assert.Equal(0, gps.HandlerCount);
    }


    [Fact]
    public async Task Cancelled_DetachesTheHandler_AndReleasesTheLock()
    {
        var gps = new FakeGpsManager();
        using var cts = new CancellationTokenSource();
        var task = gps.GetCurrentPosition(cts.Token);
        await WaitFor(() => gps.CurrentListener != null);

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(0, gps.HandlerCount);

        // a second call gets the lock back
        var second = gps.GetCurrentPosition();
        await WaitFor(() => gps.CurrentListener != null);
        gps.Raise(Reading());
        Assert.NotNull(await second);
    }


    static GpsReading Reading() => new(new Position(43.6, -79.4), 5, DateTimeOffset.UtcNow, 0, 0, 0, 0, 0);


    static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 100 && !condition(); i++)
            await Task.Delay(20);

        Assert.True(condition());
    }


    sealed class FakeGpsManager : IGpsManager
    {
        EventHandler<GpsReading>? handlers;

        public Exception? StartException { get; init; }
        public int HandlerCount => this.handlers?.GetInvocationList().Length ?? 0;
        public GpsRequest? CurrentListener { get; private set; }

        public AccessState GetCurrentStatus(GpsRequest request) => AccessState.Available;
        public Task<AccessState> RequestAccess(GpsRequest request) => Task.FromResult(AccessState.Available);
        public Task<GpsReading?> GetLastReading(TimeSpan? timeout = null) => Task.FromResult<GpsReading?>(null);

        public event EventHandler<GpsReading> GpsReadingReceived
        {
            add => this.handlers += value;
            remove => this.handlers -= value;
        }

        public void Raise(GpsReading reading) => this.handlers?.Invoke(this, reading);

        public Task StartListener(GpsRequest request)
        {
            if (this.StartException != null)
                throw this.StartException;

            this.CurrentListener = request;
            return Task.CompletedTask;
        }

        public Task StopListener()
        {
            this.CurrentListener = null;
            return Task.CompletedTask;
        }
    }
}
