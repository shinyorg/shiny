Title: GeoDispatch
---

Push + GPS - By sending a push notification with incident data in the payload, your users will now receive a notification if they are within the sent "geofence" to ask them if they are able to manage a situation.

```csharp
namespace Shiny.GeoDispatch
{
    public interface IGeoDispatchManager
    {
        bool IsEnabled { get; set; }
        Task<PushAccessState> RequestAccess();

        Task RespondToDispatch(string identifier, bool accept, string? textReply);
        Task<IEnumerable<GeoDispatchMessage>> GetPendingDispatches();
    }
}
```

### Configuration
```csharp
    public ChannelImportance NotificationChannelImportance { get; set; } = ChannelImportance.Normal;
    public string NotificationAcceptText { get; set; } = "Accept";
    public string NotificationRejectText { get; set; } = "Reject";
    public bool AllowAcceptTextReplies { get; set; }
    public bool AllowRejectTextReplies { get; set; }

    /// <summary>
    /// If true, unit is processed as meters else processed as miles
    /// </summary>
    public bool PayloadRadiusInMetricUnit { get; set; } = true;
    public string PayloadIdKey { get; set; } = "id";
    public string PayloadLatitudeKey { get; set; } = "lat";
    public string PayloadLongitudeKey { get; set; } = "lng";
    public string PayloadRadiusKey { get; set; } = "rad";
```