using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Pages.BLE;
using Sample.Shared.Maui.Services;
using Shiny.BluetoothLE.AccessorySetup;

namespace Sample.Shared.Maui.Delegates;

/// <summary>
/// Records grant changes made outside the app's own calls - a removal in Settings, a rename on the sheet (iOS) and
/// presence wake-ups (Android 12+) - so they show on the Accessory Setup page and the Events page.
/// </summary>
public class SampleAccessorySetupDelegate(
    ILogger<SampleAccessorySetupDelegate> logger,
    IEventStore events
) : IAccessorySetupDelegate
{
    public Task OnAccessoryAdded(SetupAccessory accessory)
        => this.Log("Delegate: accessory added", accessory);

    public Task OnAccessoryRemoved(SetupAccessory accessory)
        => this.Log("Delegate: accessory removed", accessory);

    public Task OnAccessoryChanged(SetupAccessory accessory)
        => this.Log("Delegate: accessory changed", accessory);

    public Task OnPresenceChanged(SetupAccessory accessory, bool isNearby)
        => this.Log(isNearby ? "Delegate: accessory came into range" : "Delegate: accessory left range", accessory);


    Task Log(string description, SetupAccessory accessory)
    {
        logger.LogInformation("{Description} - {Accessory}", description, accessory);
        return events.Add(SampleAccessory.EventCategory, $"{description}: {accessory.DisplayName ?? accessory.Identifier}", Metadata(accessory));
    }


    internal static Dictionary<string, string?> Metadata(SetupAccessory accessory) => new()
    {
        ["Identifier"] = accessory.Identifier,
        ["DisplayName"] = accessory.DisplayName,
        ["PeripheralUuid"] = accessory.PeripheralUuid,
        ["State"] = accessory.State.ToString(),
        ["MacAddress"] = accessory.MacAddress
    };
}
