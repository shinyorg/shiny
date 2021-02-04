using Shiny.Attributes;

// TODO: cannot have push
[assembly: AutoStartup("Shiny.Notifications.INotificationDelegate", "services.UseNotifications", true)]

[assembly: StaticGeneration("Shiny.Notifications.INotificationManager", "ShinyNotifications")]

