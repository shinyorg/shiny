using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Beacons;
using Shiny.Notifications;

namespace Sample.Shared.Maui.Delegates;

public class SampleBeaconMonitorDelegate(
    ILogger<SampleBeaconMonitorDelegate> logger,
    INotificationManager notificationManager,
    IEventStore events
) : IBeaconMonitorDelegate
{
    public async Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region)
    {
        var msg = $"Beacon region status: {newStatus}, region: {region.Identifier}";
        logger.LogInformation(msg);

        await events.Add(
            "Beacon",
            msg,
            new Dictionary<string, string?>
            {
                ["Identifier"] = region.Identifier,
                ["Status"] = newStatus.ToString(),
                ["Uuid"] = region.Uuid.ToString(),
                ["Major"] = region.Major?.ToString(),
                ["Minor"] = region.Minor?.ToString()
            }
        );
        await notificationManager.Send("Shiny Beacons", msg);
    }
}
