namespace Shiny.Notifications;

public record NotificationAccessState(
    AccessState PostNotifications,
    AccessState? LocationAware,
    AccessState? TimeSensitivity
);
