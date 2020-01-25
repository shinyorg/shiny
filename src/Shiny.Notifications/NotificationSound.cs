using System;


namespace Shiny.Notifications
{
    public struct NotificationSound
    {
        NotificationSound(string path) => this.Path = path;
        public string Path { get; }

        public static NotificationSound None { get; } = new NotificationSound(nameof(None));
        public static NotificationSound DefaultSystem { get; } = new NotificationSound(nameof(DefaultSystem));
        public static NotificationSound DefaultPriority { get; } = new NotificationSound(nameof(DefaultPriority));
        public static NotificationSound FromCustom(string path) => new NotificationSound(path);

        public override int GetHashCode() => this.Path.GetHashCode();
        public override bool Equals(object obj)
        {
            if (obj is NotificationSound sound)
                return this.Path.Equals(sound.Path);
            return false;
        }
    }
}
