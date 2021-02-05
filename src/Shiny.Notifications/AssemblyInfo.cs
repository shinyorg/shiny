using Shiny.Attributes;

// TODO: cannot have push
[assembly: AutoStartupWithDelegate("Shiny.Notifications.INotificationDelegate", "UseNotifications", false)]

[assembly: StaticGeneration("Shiny.Notifications.INotificationManager", "ShinyNotifications")]

