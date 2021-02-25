using Shiny.Attributes;

[assembly: AutoStartupWithDelegate("Shiny.Notifications.INotificationDelegate", "UseNotifications", false)]
[assembly: StaticGeneration("Shiny.Notifications.INotificationManager", "ShinyNotifications")]

