using Sample.tvOS.Infrastructure;
using Shiny.Net.Discovery;

namespace Sample.tvOS.Pages;


/// <summary>
/// mDNS/DNS-SD over NSNetService - the same Bonjour path iOS takes, so no multicast entitlement
/// is needed. An Apple TV is usually the device doing the discovering on a home network, which
/// makes this the most natural module of the set here.
/// </summary>
public class DiscoveryViewController() : ModuleViewController(
    "Shiny.Net.Discovery - Bonjour via NSNetService. Needs NSBonjourServices + NSLocalNetworkUsageDescription"
)
{
    const string ServiceType = "_http._tcp";

    CancellationTokenSource? browse;
    IMdnsPublication? publication;


    protected override void OnReady()
    {
        this.AddAction($"Browse {ServiceType}", () =>
        {
            if (this.browse != null)
            {
                this.Log("already browsing");
                return Task.CompletedTask;
            }

            this.browse = new CancellationTokenSource();
            var ct = this.browse.Token;
            this.ClearLog();
            this.Log($"browsing {ServiceType}.local ...");

            _ = Task.Run(async () =>
            {
                try
                {
                    var mdns = Resolve<IMdnsManager>();
                    await foreach (var result in mdns.Browse(new MdnsBrowseConfig(ServiceType), ct))
                    {
                        var svc = result.Service;
                        var where = svc.IsResolved
                            ? $"{svc.HostName}:{svc.Port} [{String.Join(", ", svc.Addresses)}]"
                            : "(unresolved)";

                        this.Log($"{result.Status,-5} {svc.InstanceName}  {where}");
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    this.Log($"browse error: {ex.Message}");
                }
            }, ct);

            return Task.CompletedTask;
        });

        this.AddAction("Stop", () =>
        {
            this.browse?.Cancel();
            this.browse?.Dispose();
            this.browse = null;
            this.Log("browse stopped");
            return Task.CompletedTask;
        });

        this.AddAction("Advertise", async () =>
        {
            if (this.publication != null)
            {
                this.Log("already advertising");
                return;
            }

            var mdns = Resolve<IMdnsManager>();
            this.publication = await mdns.Publish(new MdnsServiceRegistration
            {
                InstanceName = "Shiny Apple TV",
                ServiceType = ServiceType,
                Port = 8080,
                TxtRecords = new Dictionary<string, string>
                {
                    ["path"] = "/",
                    ["device"] = "appletv"
                }
            });
            this.Log($"advertising as '{this.publication.InstanceName}' on port 8080");
            this.Log("nothing is actually listening on 8080 - Publish only advertises");
        });

        this.AddAction("Withdraw", async () =>
        {
            if (this.publication == null)
            {
                this.Log("not advertising");
                return;
            }

            await this.publication.DisposeAsync();
            this.publication = null;
            this.Log("goodbye packet sent, advertisement withdrawn");
        });
    }
}
