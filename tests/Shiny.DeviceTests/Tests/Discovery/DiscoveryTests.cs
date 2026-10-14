using System.Xml;
using Shiny.Net.Discovery;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Self-loop discovery: the device publishes a service and then finds it again with its own browser, so no
/// second device or runner is needed.
///
/// mDNS goes through NSNetService / NsdManager and runs everywhere. SSDP and WS-Discovery are raw multicast:
/// on a physical iPhone that needs com.apple.developer.networking.multicast, which this app is not provisioned
/// with, so those tests skip there. The iOS simulator uses the Mac's network stack and does not enforce the
/// entitlement (the discovery skill notes it "cannot be tested in the simulator" for the same reason), so they
/// run there, as they do on Android and Mac Catalyst (sandbox network client/server entitlements).
/// </summary>
public class DiscoveryTests(
    IMdnsManager mdns,
    ISsdpManager ssdp,
    IWsDiscoveryManager wsd
) : DeviceTestSuite
{
    public const string ServiceType = "_shinytest._tcp";
    const string SsdpDeviceType = "urn:schemas-shinylib-net:device:ShinyDeviceTest:1";
    static readonly XmlQualifiedName WsdType = new("ShinyDeviceTest", "urn:shinylib:devicetests");

    public override string Name => "Discovery";
    protected override TimeSpan DefaultTimeout => TimeSpan.FromSeconds(90);


    protected override void Define()
    {
        this.Test("Mdns_PublishBrowseResolve", async ctx =>
        {
            var id = NewId();
            var port = 50000 + Random.Shared.Next(10000);
            await using var publication = await mdns.Publish(
                new MdnsServiceRegistration($"Shiny Test {id}", ServiceType, port)
                {
                    TxtRecords = new Dictionary<string, string> { ["suite"] = "discovery", ["id"] = id }
                },
                ctx.CancellationToken
            );
            ctx.Log($"Published '{publication.InstanceName}' {publication.ServiceType} :{publication.Port}");
            Assert.Equal(port, publication.Port);
            Assert.StartsWith($"Shiny Test {id}", publication.InstanceName);

            var found = await FirstAsync(
                mdns.Browse(new MdnsBrowseConfig(ServiceType), ctx.CancellationToken),
                x => x.Status == MdnsBrowseStatus.Found && x.Service.InstanceName == publication.InstanceName && x.Service.IsResolved,
                TimeSpan.FromSeconds(30),
                ctx.CancellationToken
            );
            ctx.Log($"Browsed {found.Service}");
            Assert.Equal(port, found.Service.Port);
            Assert.Equal(id, found.Service.TxtRecords["id"]);
            Assert.Equal("discovery", found.Service.TxtRecords["suite"]);
            Assert.NotEmpty(found.Service.Addresses);
            Assert.False(String.IsNullOrWhiteSpace(found.Service.HostName));

            var resolved = await mdns.Resolve(publication.InstanceName, ServiceType, TimeSpan.FromSeconds(15), ctx.CancellationToken);
            Assert.NotNull(resolved);
            ctx.Log($"Resolved {resolved}");
            Assert.True(resolved.IsResolved);
            Assert.Equal(port, resolved.Port);
            Assert.Equal(id, resolved.TxtRecords["id"]);
        });


        this.Test("Mdns_UnpublishReportsLost", async ctx =>
        {
            var id = NewId();
            var publication = await mdns.Publish(
                new MdnsServiceRegistration($"Shiny Lost {id}", ServiceType, 50000 + Random.Shared.Next(10000)),
                ctx.CancellationToken
            );
            var name = publication.InstanceName;

            var seen = await BrowseUntilGoneAsync(
                mdns.Browse(new MdnsBrowseConfig(ServiceType) { ResolveServices = false }, ctx.CancellationToken),
                x => x.Service.InstanceName == name,
                x => x.Status == MdnsBrowseStatus.Found,
                () => publication.DisposeAsync().AsTask(),
                ctx
            );
            Assert.Equal(MdnsBrowseStatus.Lost, seen.Status);
        });


        this.Test("Ssdp_PublishAndSearch", async ctx =>
        {
            SkipWithoutMulticast(ctx);
            var udn = "uuid:" + Guid.NewGuid();
            var location = new Uri($"http://127.0.0.1:{50000 + Random.Shared.Next(10000)}/shiny-devicetest.xml");

            await using var publication = await ssdp.Publish(
                new SsdpDeviceRegistration(udn, location)
                {
                    DeviceType = SsdpDeviceType,
                    Server = "ShinyDeviceTests/1.0 UPnP/1.1"
                },
                ctx.CancellationToken
            );
            ctx.Log($"Published {publication.Udn} at {publication.Location}: {String.Join(", ", publication.NotificationTypes)}");
            Assert.Equal(udn, publication.Udn);

            var found = await FirstAsync(
                ssdp.Browse(new SsdpBrowseConfig(SsdpDeviceType) { MaxWait = TimeSpan.FromSeconds(2), SearchInterval = TimeSpan.FromSeconds(5) }, ctx.CancellationToken),
                x => x.Status == SsdpBrowseStatus.Found && x.Device.Udn == udn,
                TimeSpan.FromSeconds(30),
                ctx.CancellationToken
            );
            ctx.Log($"Found {found.Device}");
            Assert.Equal(location, found.Device.Location);
            Assert.Contains(SsdpDeviceType, found.Device.NotificationTypes);
        });


        this.Test("Ssdp_UnpublishSendsByeBye", async ctx =>
        {
            SkipWithoutMulticast(ctx);
            var udn = "uuid:" + Guid.NewGuid();
            var publication = await ssdp.Publish(
                new SsdpDeviceRegistration(udn, new Uri("http://127.0.0.1:1/shiny-devicetest.xml")) { DeviceType = SsdpDeviceType },
                ctx.CancellationToken
            );

            var seen = await BrowseUntilGoneAsync(
                ssdp.Browse(new SsdpBrowseConfig(SsdpDeviceType) { MaxWait = TimeSpan.FromSeconds(2), SearchInterval = TimeSpan.FromSeconds(5) }, ctx.CancellationToken),
                x => x.Device.Udn == udn,
                x => x.Status == SsdpBrowseStatus.Found,
                () => publication.DisposeAsync().AsTask(),
                ctx
            );
            Assert.Equal(SsdpBrowseStatus.Lost, seen.Status);
        });


        this.Test("WsDiscovery_PublishProbeResolve", async ctx =>
        {
            SkipWithoutMulticast(ctx);
            var epr = "urn:uuid:" + Guid.NewGuid();
            var scope = $"http://shinylib.net/devicetests/{NewId()}";
            var address = new Uri("http://127.0.0.1:1/shiny-devicetest");

            await using var publication = await wsd.Publish(
                new WsDiscoveryRegistration(epr)
                {
                    Types = [WsdType],
                    Scopes = [scope],
                    Addresses = [address]
                },
                ctx.CancellationToken
            );
            Assert.Equal(epr, publication.EndpointReference);

            var found = await FirstAsync(
                wsd.Browse(new WsdProbeConfig(WsdType) { ProbeInterval = TimeSpan.FromSeconds(5) }, ctx.CancellationToken),
                x => x.Status == WsDiscoveryStatus.Found && x.Target.EndpointReference == epr,
                TimeSpan.FromSeconds(30),
                ctx.CancellationToken
            );
            ctx.Log($"Probed {found.Target}");
            Assert.Contains(WsdType, found.Target.Types);
            Assert.Contains(scope, found.Target.Scopes);
            Assert.Contains(address, found.Target.Addresses);

            var resolved = await wsd.Resolve(epr, TimeSpan.FromSeconds(10), ctx.CancellationToken);
            Assert.NotNull(resolved);
            Assert.Equal(epr, resolved.EndpointReference);
            Assert.Contains(address, resolved.Addresses);
        });


        this.Test("WsDiscovery_UnpublishSendsBye", async ctx =>
        {
            SkipWithoutMulticast(ctx);
            var epr = "urn:uuid:" + Guid.NewGuid();
            var publication = await wsd.Publish(
                new WsDiscoveryRegistration(epr) { Types = [WsdType], Addresses = [new Uri("http://127.0.0.1:1/shiny-devicetest")] },
                ctx.CancellationToken
            );

            var seen = await BrowseUntilGoneAsync(
                wsd.Browse(new WsdProbeConfig(WsdType) { ProbeInterval = TimeSpan.FromSeconds(5) }, ctx.CancellationToken),
                x => x.Target.EndpointReference == epr,
                x => x.Status == WsDiscoveryStatus.Found,
                () => publication.DisposeAsync().AsTask(),
                ctx
            );
            Assert.Equal(WsDiscoveryStatus.Lost, seen.Status);
        });
    }


    static void SkipWithoutMulticast(TestContext ctx)
    {
        if (ctx.Platform == "ios" && !ctx.IsSimulator)
            TestContext.Skip("Raw multicast on iOS needs com.apple.developer.networking.multicast, which this app is not provisioned with");
    }


    static string NewId() => Guid.NewGuid().ToString("N")[..8];


    // the browse streams never complete on their own - stop at the first match or the timeout
    static async Task<T> FirstAsync<T>(IAsyncEnumerable<T> source, Func<T, bool> match, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            await foreach (var item in source.WithCancellation(cts.Token).ConfigureAwait(false))
            {
                if (match(item))
                    return item;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
        }
        throw new TimeoutException($"No matching {typeof(T).Name} within {timeout.TotalSeconds:0}s");
    }


    // waits for the item to be found, runs unpublish, then returns the next result for it (expected: lost)
    static async Task<T> BrowseUntilGoneAsync<T>(
        IAsyncEnumerable<T> source,
        Func<T, bool> isOurs,
        Func<T, bool> isFound,
        Func<Task> unpublish,
        TestContext ctx
    )
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ctx.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(60));
        var unpublished = false;
        try
        {
            await foreach (var item in source.WithCancellation(cts.Token).ConfigureAwait(false))
            {
                if (isOurs(item) && !isFound(item))
                    return item;

                if (isOurs(item) && !unpublished)
                {
                    ctx.Log("Found - unpublishing");
                    unpublished = true;
                    await unpublish().ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (!ctx.CancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            if (!unpublished)
                await unpublish().ConfigureAwait(false);
        }
        throw new TimeoutException(unpublished
            ? "Unpublished, but the browser never reported the service as lost"
            : "The published service was never found");
    }
}
