namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// Hears about accessory grants changing outside the call that caused them - the user removing an accessory in
/// Settings, a rename, and (Android) the accessory coming into or leaving range. Register with
/// <c>services.AddAccessorySetup&lt;TDelegate&gt;()</c>; every registered delegate runs. Implement only what you need.
/// </summary>
public interface IAccessorySetupDelegate
{
    /// <summary>An accessory was granted to the app (also raised for one set up through <see cref="IAccessorySetupManager.ShowPicker"/>).</summary>
    Task OnAccessoryAdded(SetupAccessory accessory) => Task.CompletedTask;

    /// <summary>
    /// The grant for an accessory was removed - by the app, or by the user in Settings (iOS: Settings &gt; Privacy
    /// &amp; Security &gt; Accessories).
    /// </summary>
    Task OnAccessoryRemoved(SetupAccessory accessory) => Task.CompletedTask;

    /// <summary>iOS: an accessory's name or state changed.</summary>
    Task OnAccessoryChanged(SetupAccessory accessory) => Task.CompletedTask;

    /// <summary>
    /// Android 12+: an accessory passed to <see cref="IAccessorySetupManager.StartObservingPresence"/> came into
    /// range (<paramref name="isNearby"/> true) or left it. The system starts the app for this if it is not running.
    /// </summary>
    Task OnPresenceChanged(SetupAccessory accessory, bool isNearby) => Task.CompletedTask;
}
