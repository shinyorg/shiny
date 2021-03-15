using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shiny;
using Shiny.Notifications;
using Shiny.Stores;


namespace Samples
{
    public class NotificationRegistration
    {
        public NotificationRegistration(string description, Type type, bool hasEntryExit)
        {
            this.Description = description;
            this.Type = type;
            this.HasEntryExit = hasEntryExit;
        }


        public string Description { get; }
        public Type Type { get; }
        public bool HasEntryExit { get; }
    }


    public class AppNotifications
    {
        readonly IKeyValueStore settings;
        readonly INotificationManager notifications;
        readonly Dictionary<Type, NotificationRegistration> registrations;


        public AppNotifications(IKeyValueStoreFactory storeFactory, INotificationManager notifications)
        {
            this.settings = storeFactory.GetStore("settings");
            this.notifications = notifications;
            this.registrations = new Dictionary<Type, NotificationRegistration>();
        }


        public NotificationRegistration[] GetRegistrations()
            => this.registrations.Values.OrderBy(x => x.Description).ToArray();


        public void Set(Type type, bool entry, bool enabled)
            => this.settings.Set(ToKey(type, entry), enabled);


        public void Register(Type type, bool hasEntryExit, string description)
        {
            if (this.registrations.ContainsKey(type))
                return;

            this.registrations.Add(type, new NotificationRegistration(description, type, hasEntryExit));
        }


        public bool IsEnabled(Type type, bool entry) => this.settings.Get<bool>(ToKey(type, entry));


        public async Task Send(Type type, bool entry, string title, string message)
        {
            if (IsEnabled(type, entry))
                await this.notifications.Send(title, message);
        }


        static string ToKey(Type type, bool entry)
        {
            var e = entry ? "Entry" : "Exit";
            var key = $"{type.FullName}.{e}";
            return key;
        }
    }
}
