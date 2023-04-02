using System;
using System.Collections.Generic;
using Shiny.Stores;
using Shiny.Support.Repositories.Impl;

namespace Shiny.Notifications.Infrastructure;


public class NotificationRepositoryConverter : RepositoryConverter<Notification>
{
    public override Notification FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var result = new Notification
        {
            Id = Int32.Parse((string)values[nameof(Notification.Identifier)]),
            Message = (string)values[nameof(Notification.Message)],
            Channel = (string)values[nameof(Notification.Channel)],
            Title = this.ConvertFromStoreValue<string>(values, nameof(Notification.Title)),
            Thread = this.ConvertFromStoreValue<string>(values, nameof(Notification.Thread)),
            LocalAttachmentPath = this.ConvertFromStoreValue<string>(values, nameof(Notification.LocalAttachmentPath)),
            BadgeCount = this.ConvertFromStoreValue<int?>(values, nameof(Notification.BadgeCount)),
            ScheduleDate = this.ConvertFromStoreValue<DateTimeOffset?>(values, nameof(Notification.ScheduleDate))
        };
        if (values.ContainsKey(nameof(Notification.Payload)))
            result.Payload = serializer.Deserialize<Dictionary<string, string>>((string)values[nameof(Notification.Payload)]);

        if (values.ContainsKey(nameof(Notification.Geofence)))
            result.Geofence = serializer.Deserialize<GeofenceTrigger>((string)values[nameof(Notification.Geofence)]);

        if (values.ContainsKey(nameof(Notification.RepeatInterval)))
            result.RepeatInterval = serializer.Deserialize<IntervalTrigger>((string)values[nameof(Notification.RepeatInterval)]);

        return result;
    }


    public override IEnumerable<(string Property, object Value)> ToStore(Notification entity, ISerializer serializer)
    {
        yield return (nameof(entity.Channel), entity.Channel!);
        yield return (nameof(entity.Message), entity.Message!);

        if (entity.BadgeCount != null)
            yield return (nameof(entity.BadgeCount), entity.BadgeCount!);

        if (entity.Title != null)
            yield return (nameof(entity.Title), entity.Title);

        if (entity.Thread != null)
            yield return (nameof(entity.Thread), entity.Thread);

        if (entity.LocalAttachmentPath != null)
            yield return (nameof(entity.LocalAttachmentPath), entity.LocalAttachmentPath);

        if ((entity.Payload?.Count) > 0)
            yield return (nameof(entity.Payload), this.ConvertToStoreValue(entity.Payload));

        if (entity.Geofence != null)
            yield return (nameof(entity.Geofence), serializer.Serialize(entity.Geofence));

        if (entity.RepeatInterval != null)
            yield return (nameof(entity.RepeatInterval), serializer.Serialize(entity.RepeatInterval));
    }
}
