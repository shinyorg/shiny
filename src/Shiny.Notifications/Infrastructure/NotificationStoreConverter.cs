using System;
using System.Collections.Generic;
using System.Text.Json;
using Shiny.Stores;

namespace Shiny.Notifications.Infrastructure;


public class NotificationStoreConverter : StoreConverter<Notification>
{
    public override Notification FromStore(IDictionary<string, object> values)
    {
        var result = new Notification
        {
            Id = Int32.Parse((string)values[nameof(Notification.Identifier)]),
            Message = (string)values[nameof(Notification.Message)],
            Channel = (string)values[nameof(Notification.Channel)],
            Title = this.Get<string>(values, nameof(Notification.Title)),
            Thread = this.Get<string>(values, nameof(Notification.Thread)),
            ImageUri = this.Get<string>(values, nameof(Notification.ImageUri)),
            BadgeCount = this.Get<int>(values, nameof(Notification.BadgeCount), 0),
            ScheduleDate = this.Get<DateTimeOffset>(values, nameof(Notification.ScheduleDate))
        };
        if (values.ContainsKey(nameof(Notification.Geofence)))
            result.Geofence = JsonSerializer.Deserialize<GeofenceTrigger>((string)values[nameof(Notification.Geofence)]);

        if (values.ContainsKey(nameof(Notification.RepeatInterval)))
            result.RepeatInterval = JsonSerializer.Deserialize<IntervalTrigger>((string)values[nameof(Notification.RepeatInterval)]);

        return result;
    }


    public override IEnumerable<(string Property, object Value)> ToStore(Notification entity)
    {
        yield return (nameof(entity.Channel), entity.Channel!);
        yield return (nameof(entity.Message), entity.Message!);

        if (entity.Title != null)
            yield return (nameof(entity.Title), entity.Title);

        if (entity.Thread != null)
            yield return (nameof(entity.Thread), entity.Thread);

        if (entity.ImageUri != null)
            yield return (nameof(entity.ImageUri), entity.ImageUri);

        if ((entity.Payload?.Count) > 0)
            yield return (nameof(entity.Payload), this.ConvertToStoreValue(entity.Payload));
        
        if (entity.Geofence != null)
            yield return (nameof(entity.Geofence), JsonSerializer.Serialize(entity.Geofence));

        if (entity.RepeatInterval != null)
            yield return (nameof(entity.RepeatInterval), JsonSerializer.Serialize(entity.RepeatInterval));
    }
}
