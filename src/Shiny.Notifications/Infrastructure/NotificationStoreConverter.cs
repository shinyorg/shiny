using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Notifications.Infrastructure;


public class NotificationStoreConverter : IStoreConverter<Notification>
{
    public Notification FromStore(IDictionary<string, object> values)
    {
        var result = new Notification
        {
            Id = (int)values["Identifier"],
            Message = (string)values[nameof(Notification.Message)],
            Channel = (string)values[nameof(Notification.Channel)],
            BadgeCount = (int)values[nameof(Notification.BadgeCount)]
        };
        
        if (values.ContainsKey(nameof(Notification.Title)))
            result.Title = (string)values[nameof(Notification.Title)];

        if (values.ContainsKey(nameof(Notification.Thread)))
            result.Thread = (string)values[nameof(Notification.Thread)];

        if (values.ContainsKey(nameof(Notification.ImageUri)))
            result.ImageUri = (string)values[nameof(Notification.ImageUri)];

        //if (values.Contains(nameof(Notification.Geofence)))
        //if (values.Contains(nameof(Notification.RepeatInterval)))
        return result;
    }


    public IEnumerable<(string Property, object Value)> ToStore(Notification entity)
    {
        yield return (nameof(entity.Identifier), entity.Id.ToString());
        yield return (nameof(entity.Channel), entity.Channel!);
        yield return (nameof(entity.Message), entity.Message!);

        if (entity.Title != null)
            yield return (nameof(entity.Title), entity.Title);

        if (entity.Thread != null)
            yield return (nameof(entity.Thread), entity.Thread);

        if (entity.ImageUri != null)
            yield return (nameof(entity.ImageUri), entity.ImageUri);

        // I don't really want this here - maybe consider platform interceptors of some sort
#if ANDROID
        //result.Android
#endif

    }
}
