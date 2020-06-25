using System;


namespace Shiny.Notifications
{
    public enum NotificationSoundType
    {
        None,
        Default,
        Priority,
        Custom
    }


    public class NotificationSound
    {
        public NotificationSoundType Type { get; set; } = NotificationSoundType.None;
        public string? CustomPath { get; set; }


        public static NotificationSound FromCustom(string path) => new NotificationSound
        {
            Type = NotificationSoundType.Custom,
            CustomPath = path
        };

        public static NotificationSound None { get; } = new NotificationSound { Type = NotificationSoundType.None };
        public static NotificationSound Default { get; } = new NotificationSound { Type = NotificationSoundType.Default };
    }
}
