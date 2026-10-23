using Xunit;

namespace Shiny.Net.Vpn.Tests;


public class VpnManagerTests
{
    const VpnCapabilities Everything =
        VpnCapabilities.Profile |
        VpnCapabilities.Connect |
        VpnCapabilities.Disconnect |
        VpnCapabilities.Status |
        VpnCapabilities.SharedSecretAuthentication |
        VpnCapabilities.UsernamePasswordAuthentication |
        VpnCapabilities.CertificateAuthentication |
        VpnCapabilities.ServerRootCertificate |
        VpnCapabilities.RemoteIdentifier;


    static VpnProfile Profile(VpnAuthentication? auth = null) => new()
    {
        Name = "Test",
        ServerAddress = "vpn.example.com",
        Authentication = auth ?? VpnAuthentication.SharedSecret("secret")
    };


    [Fact]
    public async Task ConnectWaitsThroughConnectingToConnected()
    {
        var vpn = new FakeVpnManager(Everything);
        vpn.AfterStart.AddRange([VpnConnectionState.Disconnected, VpnConnectionState.Connecting, VpnConnectionState.Connecting, VpnConnectionState.Connected]);

        await vpn.Connect(TimeSpan.FromSeconds(5));

        Assert.Equal(1, vpn.Starts);
        Assert.Equal(VpnConnectionState.Connected, await vpn.GetStatus());
    }


    /// <remarks>
    /// The OS takes a moment to leave Disconnected after the start call. Treating that first read as a
    /// failure would fail every connect on a slow device.
    /// </remarks>
    [Fact]
    public async Task ADisconnectedReadBeforeTheAttemptIsSeenIsNotAFailure()
    {
        var vpn = new FakeVpnManager(Everything);
        vpn.AfterStart.AddRange([VpnConnectionState.Disconnected, VpnConnectionState.Disconnected, VpnConnectionState.Connected]);

        await vpn.Connect(TimeSpan.FromSeconds(5));
        Assert.Equal(1, vpn.Starts);
    }


    [Fact]
    public async Task FallingBackToDisconnectedAfterConnectingFails()
    {
        var vpn = new FakeVpnManager(Everything);
        vpn.AfterStart.AddRange([VpnConnectionState.Connecting, VpnConnectionState.Disconnected]);

        var ex = await Assert.ThrowsAsync<VpnConnectionException>(() => vpn.Connect(TimeSpan.FromSeconds(5)));
        Assert.Contains("gave no reason", ex.Message);
    }


    /// <remarks>
    /// A refused login can fail before the attempt is ever seen in progress - the reason the platform
    /// reported is what tells it apart from "not started yet", so it fails straight away.
    /// </remarks>
    [Fact]
    public async Task APlatformReasonFailsWithoutWaitingForTheTimeout()
    {
        var vpn = new FakeVpnManager(Everything) { ReportFailureOnStart = "the server rejected the login" };
        vpn.AfterStart.Add(VpnConnectionState.Disconnected);

        var ex = await Assert.ThrowsAsync<VpnConnectionException>(() => vpn.Connect(TimeSpan.FromSeconds(30)));
        Assert.Contains("the server rejected the login", ex.Message);
    }


    [Fact]
    public async Task ATunnelThatNeverComesUpTimesOutAndIsStopped()
    {
        var vpn = new FakeVpnManager(Everything);
        vpn.AfterStart.Add(VpnConnectionState.Connecting);

        var ex = await Assert.ThrowsAsync<VpnConnectionException>(() => vpn.Connect(TimeSpan.FromMilliseconds(100)));
        Assert.Contains("did not connect within", ex.Message);
        Assert.Equal(1, vpn.Stops);
    }


    [Fact]
    public async Task CallerCancellationIsNotReportedAsATimeout()
    {
        var vpn = new FakeVpnManager(Everything);
        vpn.AfterStart.Add(VpnConnectionState.Connecting);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => vpn.Connect(TimeSpan.FromSeconds(30), cts.Token));
    }


    [Fact]
    public async Task ConnectingWhenAlreadyConnectedDoesNotRestart()
    {
        var vpn = new FakeVpnManager(Everything, VpnConnectionState.Connected);
        await vpn.Connect();
        Assert.Equal(0, vpn.Starts);
    }


    [Fact]
    public async Task ConnectingWithNoProfileIsAPermissionProblem()
    {
        var vpn = new FakeVpnManager(Everything, VpnConnectionState.NotConfigured);
        await Assert.ThrowsAsync<VpnPermissionException>(() => vpn.Connect());
        Assert.Equal(0, vpn.Starts);
    }


    /// <remarks>
    /// Android 11 and 12: the OS accepts the start and will not say anything more, so Connect cannot
    /// wait - and GetStatus says Unknown rather than guessing.
    /// </remarks>
    [Fact]
    public async Task WithoutStatusConnectReturnsOnceStarted()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.Status);
        vpn.AfterStart.Add(VpnConnectionState.Connecting);

        await vpn.Connect(TimeSpan.FromMilliseconds(50));

        Assert.Equal(1, vpn.Starts);
        Assert.Equal(VpnConnectionState.Unknown, await vpn.GetStatus());
    }


    [Fact]
    public async Task DisconnectWaitsForTheTunnelToGoDown()
    {
        var vpn = new FakeVpnManager(Everything, VpnConnectionState.Connected);
        await vpn.Disconnect();

        Assert.Equal(1, vpn.Stops);
        Assert.Equal(VpnConnectionState.Disconnected, await vpn.GetStatus());
    }


    [Fact]
    public async Task AnAuthenticationThePlatformLacksIsRefusedBeforeReachingIt()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.CertificateAuthentication);

        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.SaveProfile(Profile(VpnAuthentication.Certificate([1, 2, 3]))));
        Assert.Null(vpn.Saved);
    }


    [Fact]
    public async Task ARemoteIdentifierEqualToTheServerAddressNeedsNoCapability()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.RemoteIdentifier);
        var profile = new VpnProfile
        {
            Name = "Test",
            ServerAddress = "vpn.example.com",
            RemoteIdentifier = "VPN.example.com",
            Authentication = VpnAuthentication.SharedSecret("secret")
        };

        Assert.Equal(AccessState.Available, await vpn.SaveProfile(profile));
    }


    [Fact]
    public async Task ADifferentRemoteIdentifierNeedsTheCapability()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.RemoteIdentifier);
        var profile = new VpnProfile
        {
            Name = "Test",
            ServerAddress = "vpn.example.com",
            RemoteIdentifier = "server-id",
            Authentication = VpnAuthentication.SharedSecret("secret")
        };

        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.SaveProfile(profile));
    }


    [Fact]
    public async Task AServerRootCertificateNeedsTheCapability()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.ServerRootCertificate);
        var profile = new VpnProfile
        {
            Name = "Test",
            ServerAddress = "vpn.example.com",
            ServerRootCertificate = [1, 2, 3],
            Authentication = VpnAuthentication.UsernamePassword("user", "pass")
        };

        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.SaveProfile(profile));
    }


    [Fact]
    public async Task ABlankServerAddressIsRejected()
    {
        var vpn = new FakeVpnManager(Everything);
        var profile = new VpnProfile
        {
            Name = "Test",
            ServerAddress = " ",
            Authentication = VpnAuthentication.SharedSecret("secret")
        };

        await Assert.ThrowsAsync<ArgumentException>(() => vpn.SaveProfile(profile));
    }


    [Fact]
    public async Task ADeclinedConsentIsReturnedNotThrown()
    {
        var vpn = new FakeVpnManager(Everything) { SaveResult = AccessState.Denied };
        Assert.Equal(AccessState.Denied, await vpn.SaveProfile(Profile()));
    }


    [Fact]
    public async Task APlatformWithNothingThrowsNotSupported()
    {
        var vpn = new FakeVpnManager(VpnCapabilities.None);

        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.SaveProfile(Profile()));
        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.Connect());
        await Assert.ThrowsAsync<VpnNotSupportedException>(() => vpn.RemoveProfile());
    }


    /// <remarks>
    /// The secrets must not reach a log line through ToString - the reason these are classes, not records.
    /// </remarks>
    [Fact]
    public void AuthenticationNeverFormatsItsSecret()
    {
        Assert.DoesNotContain("hunter2", VpnAuthentication.SharedSecret("hunter2").ToString());
        Assert.DoesNotContain("hunter2", VpnAuthentication.UsernamePassword("allan", "hunter2").ToString());
        Assert.DoesNotContain("hunter2", VpnAuthentication.Certificate([1], "hunter2").ToString());
    }


    [Fact]
    public async Task SubscribingDeliversTheCurrentStateThenOnlyChanges()
    {
        var vpn = new FakeVpnManager(Everything);
        var seen = new List<VpnConnectionState>();
        var gotOne = new SemaphoreSlim(0);
        EventHandler<VpnConnectionState> handler = (_, s) =>
        {
            lock (seen)
                seen.Add(s);
            gotOne.Release();
        };

        vpn.StatusChanged += handler;
        Assert.True(await gotOne.WaitAsync(TimeSpan.FromSeconds(5)));

        vpn.SetState(VpnConnectionState.Disconnected);   // same as before - swallowed
        vpn.SetState(VpnConnectionState.Connecting);
        Assert.True(await gotOne.WaitAsync(TimeSpan.FromSeconds(5)));

        vpn.StatusChanged -= handler;

        Assert.Equal([VpnConnectionState.Disconnected, VpnConnectionState.Connecting], seen);
        Assert.Equal(1, vpn.ListenStarts);
        Assert.Equal(1, vpn.ListenStops);
    }


    [Fact]
    public void WithoutStatusSubscribingStartsNoWatcher()
    {
        var vpn = new FakeVpnManager(Everything & ~VpnCapabilities.Status);
        EventHandler<VpnConnectionState> handler = (_, _) => { };

        vpn.StatusChanged += handler;
        vpn.StatusChanged -= handler;

        Assert.Equal(0, vpn.ListenStarts);
    }
}
