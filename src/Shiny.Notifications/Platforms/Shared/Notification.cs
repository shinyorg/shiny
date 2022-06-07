using System;
using System.Collections.Generic;

using Shiny.Stores;

namespace Shiny.Notifications;


public class Notification : IStoreEntity
{
    public static string? DefaultChannel { get; set; }

    public static string? DefaultTitle { get; set; }

    /// <summary>
    /// You do not have to set this - it will be automatically set from the library if you do not supply one
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The title of the message
    /// </summary>
    public string? Title { get; set; } = DefaultTitle;

    /// <summary>
    /// The body of the notification - can be blank
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The channel ID
    /// </summary>
    public string? Channel { get; set; } = DefaultChannel;

    /// <summary>
    /// Additional data you can add to your notification
    /// </summary>
    public IDictionary<string, string>? Payload { get; set; } = new Dictionary<string, string>(0);

    /// <summary>
    /// The value to display on the homescreen badge - set to 0z to remove it
    /// Android does auto-increment this (so this value would override)
    /// iOS/UWP require you set this if you want a badge
    /// </summary>
    public int? BadgeCount { get; set; }

    /// <summary>
    /// iOS: Thread, Android: Group
    /// </summary>
    public string? Thread { get; set; }

    /// <summary>
    /// Scheduled date for notification (cannot be mixed with repeat interval or geofence)
    /// </summary>
    public DateTimeOffset? ScheduleDate { get; set; }

    /// <summary>
    /// Set the location aware geofence (cannot be mixed with repeat interval or schedule date)
    /// </summary>
    public GeofenceTrigger? Geofence { get; set; }

    /// <summary>
    /// Set the repeating interval (cannot be mixed with geofence or schedule date)
    /// </summary>
    public IntervalTrigger? RepeatInterval { get; set; }

    /// <summary>
    /// Set an image uri for iOS attachment or Android bigpicture style
    /// </summary>
    public string? ImageUri { get; set; }

    public string Identifier => this.Id.ToString();
}