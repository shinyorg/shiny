using Android.App;
using Android.Gms.Wearable;

namespace Shiny.Wearables;


/// <summary>
/// The Data Layer's way into the app: Google Play services binds it for anything under <c>/shiny</c>, waking the app
/// when it is not running. It hands everything to <see cref="WearableManager"/>, which the Shiny host has started by
/// the time a service is created.
/// <para>
/// The capability-changed filter matches too because the capability URI is <c>wear://*/shiny_wearable</c>, which the
/// <c>/shiny</c> prefix covers.
/// </para>
/// </summary>
[Android.App.Service(Exported = true)]
[IntentFilter(
    [
        "com.google.android.gms.wearable.MESSAGE_RECEIVED",
        "com.google.android.gms.wearable.REQUEST_RECEIVED",
        "com.google.android.gms.wearable.DATA_CHANGED",
        "com.google.android.gms.wearable.CAPABILITY_CHANGED"
    ],
    DataScheme = "wear",
    DataHost = "*",
    DataPathPrefix = WearableProtocol.Prefix
)]
public class ShinyWearableListenerService : WearableListenerService
{
    internal static WearableManager? Manager { get; set; }

    public override void OnMessageReceived(IMessageEvent messageEvent)
        => Manager?.OnMessage(messageEvent.Path, messageEvent.GetData() ?? [], messageEvent.SourceNodeId);

    public override Android.Gms.Tasks.Task OnRequest(string nodeId, string path, byte[] request)
    {
        var tcs = new Android.Gms.Tasks.TaskCompletionSource();
        if (Manager is { } manager)
            manager.OnRequest(nodeId, path, request ?? [], tcs);
        else
            tcs.SetException(new Java.Lang.IllegalStateException("Shiny has not started"));

        return tcs.Task;
    }

    public override void OnDataChanged(DataEventBuffer dataEvents)
        => Manager?.OnDataChanged(dataEvents);

    public override void OnCapabilityChanged(ICapabilityInfo capabilityInfo)
        => Manager?.OnCapabilityChanged();
}
