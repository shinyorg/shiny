using Microsoft.Extensions.Logging.Abstractions;

namespace Shiny.Net.Vpn.Tests;


/// <summary>
/// A platform that replays a scripted sequence of states, so the decisions AbstractVpnManager makes
/// on top of them can be tested without an OS.
/// </summary>
class FakeVpnManager : AbstractVpnManager
{
    readonly Queue<VpnConnectionState> script = new();
    VpnConnectionState last;

    public FakeVpnManager(VpnCapabilities capabilities, VpnConnectionState initial = VpnConnectionState.Disconnected)
        : base(NullLogger.Instance)
    {
        this.Capabilities = capabilities;
        this.last = initial;
        this.PollInterval = TimeSpan.FromMilliseconds(5);
    }

    public override VpnCapabilities Capabilities { get; }

    public AccessState SaveResult { get; set; } = AccessState.Available;
    public VpnProfile? Saved { get; private set; }
    public int Starts { get; private set; }
    public int Stops { get; private set; }
    public int Removes { get; private set; }
    public int ListenStarts { get; private set; }
    public int ListenStops { get; private set; }

    /// <summary>The states reported after the tunnel is started, one per read; the last one sticks.</summary>
    public List<VpnConnectionState> AfterStart { get; } = new();

    /// <summary>A reason "the platform" reports during the start, as an error callback would.</summary>
    public string? ReportFailureOnStart { get; set; }

    public void SetState(VpnConnectionState state)
    {
        this.last = state;
        this.RaiseStatusIfChanged();
    }

    protected override Task<AccessState> SaveProfileCore(VpnProfile profile, CancellationToken ct)
    {
        this.Saved = profile;
        return Task.FromResult(this.SaveResult);
    }

    protected override Task RemoveProfileCore(CancellationToken ct)
    {
        this.Removes++;
        return Task.CompletedTask;
    }

    protected override Task StartTunnel(CancellationToken ct)
    {
        this.Starts++;
        foreach (var state in this.AfterStart)
            this.script.Enqueue(state);

        if (this.ReportFailureOnStart != null)
            this.FailureReason = this.ReportFailureOnStart;

        return Task.CompletedTask;
    }

    protected override Task StopTunnel(CancellationToken ct)
    {
        this.Stops++;
        this.script.Clear();
        this.last = VpnConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    protected override Task<VpnConnectionState> GetStatusCore(CancellationToken ct)
    {
        if (this.script.Count > 0)
            this.last = this.script.Dequeue();

        return Task.FromResult(this.last);
    }

    protected override void StartListening() => this.ListenStarts++;
    protected override void StopListening() => this.ListenStops++;
}
