using System;
using System.Collections.Generic;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Notifications;


/// <summary>
/// Describes a local notification, including its content, target channel, optional payload, and optional trigger (scheduled, repeating, or geofence).
/// </summary>
public class Notification : IRepositoryEntity
{
    /// <summary>
    /// The notification identifier. If left as 0 the library will assign one automatically when the notification is sent.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The title of the notification.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The body text of the notification.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The identifier of the channel this notification belongs to.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Custom key/value data delivered with the notification and returned in the entry response.
    /// </summary>
    public IDictionary<string, string> Payload { get; set; } = new Dictionary<string, string>(0);

    /// <summary>
    /// The value to display on the app icon badge; set to 0 to clear. Android auto-increments by default and this value overrides that. iOS/UWP only show a badge when this is set.
    /// </summary>
    public int? BadgeCount { get; set; }

    /// <summary>
    /// Grouping identifier used to bundle related notifications (iOS thread identifier, Android group key).
    /// </summary>
    public string? Thread { get; set; }

    /// <summary>
    /// Schedules the notification to fire at a specific date and time. Cannot be combined with RepeatInterval or Geofence.
    /// </summary>
    public DateTimeOffset? ScheduleDate { get; set; }

    /// <summary>
    /// Triggers the notification when the device enters or exits a geofenced region. Cannot be combined with ScheduleDate or RepeatInterval.
    /// </summary>
    public GeofenceTrigger? Geofence { get; set; }

    /// <summary>
    /// Triggers the notification on a recurring interval or time-of-day. Cannot be combined with ScheduleDate or Geofence.
    /// </summary>
    public IntervalTrigger? RepeatInterval { get; set; }


    /// <summary>
    /// The repository identifier for this notification (the string form of <see cref="Id"/>).
    /// </summary>
    public string Identifier => this.Id.ToString();
}
