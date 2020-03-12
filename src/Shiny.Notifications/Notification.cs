using System;
using System.Collections.Generic;

namespace Shiny.Notifications
{

    public class Notification
    {
        public static string? DefaultCategory { get; set; }

        public static string? DefaultTitle { get; set; }

        /// <summary>
        /// This will be different per platform
        /// </summary>
        public static NotificationSound DefaultSound { get; set; } = NotificationSound.DefaultSystem;

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
        /// Scheduled date for notification - leave blank for immediate
        /// </summary>
        public DateTimeOffset? ScheduleDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Category { get; set; } = DefaultCategory;

        /// <summary>
        /// Additional data you can add to your notification
        /// </summary>
        public IDictionary<string, string>? Payload { get; set; }

        /// <summary>
        /// The value to display on the homescreen badge - set to 0z to remove it
        /// Android does auto-increment this (so this value would override)
        /// iOS/UWP require you set this if you want a badge
        /// </summary>
        public int? BadgeCount { get; set; }

        /// <summary>
        /// Options specific to android
        /// </summary>
        public AndroidOptions Android { get; set; } = new AndroidOptions();

        /// <summary>
        /// Options specific to windows (Uwp)
        /// </summary>
        public UwpOptions Windows { get; set; } = new UwpOptions();

        /// <summary>
        /// The sound you wish to use on your notification - defaults to Notification.DefaultSound
        /// </summary>
        public NotificationSound Sound { get; set; } = DefaultSound;
    }
}