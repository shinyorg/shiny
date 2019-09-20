using System;


namespace Shiny.Notifications
{

    public class Notification
    {
        public static string DefaultTitle { get; set; }

        /// <summary>
        /// This will be different per platform
        /// </summary>
        public static string CustomSoundFilePath { get; set; }

        /// <summary>
        /// You do not have to set this - it will be automatically set from the library if you do not supply one
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The title of the message
        /// </summary>
        public string Title { get; set; } = DefaultTitle;

        /// <summary>
        /// The body of the notification - can be blank
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Scheduled date for notification - leave blank for immediate
        /// </summary>
        public DateTimeOffset? ScheduleDate { get; set; }

        /// <summary>
        /// Additional data you can add to your notification
        /// </summary>
        public string Payload { get; set; }

        /// <summary>
        /// Options specific to android
        /// </summary>
        public AndroidOptions Android { get; set; } = new AndroidOptions();

        /// <summary>
        /// Options specific to windows (Uwp)
        /// </summary>
        public UwpOptions Windows { get; set; } = new UwpOptions();
    }
}