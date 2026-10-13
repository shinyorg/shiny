namespace Shiny.Notifications;

/// <summary>
/// An action button (or text reply input) attached to a notification channel.
/// </summary>
public class ChannelAction
{
    /// <summary>
    /// The unique identifier returned in <see cref="NotificationResponse.ActionIdentifier"/> when the user selects this action.
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// The text shown on the action button.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Indicates how the action behaves (e.g. plain tap, text reply, destructive, open app).
    /// </summary>
    public ChannelActionType ActionType { get; set; } = ChannelActionType.None;

    /// <summary>
    /// Requires the user to unlock the device before the action runs - use it for actions with a real-world effect
    /// (e.g. "Open Garage", "Disarm"). Combines with any <see cref="ActionType"/>.
    /// iOS/Mac Catalyst/macOS: <c>UNNotificationActionOptions.AuthenticationRequired</c>. Android: API 31+ only - older
    /// versions run the action without prompting. Ignored on Windows and Linux.
    /// </summary>
    public bool RequiresUnlock { get; set; }


    /// <summary>
    /// Creates an action whose title matches its identifier.
    /// </summary>
    /// <param name="id">The action identifier (also used as the title).</param>
    /// <param name="actionType">The action behavior.</param>
    /// <returns>A new <see cref="ChannelAction"/>.</returns>
    public static ChannelAction Create(string id, ChannelActionType actionType = ChannelActionType.None) => new ChannelAction
    {
        Identifier = id,
        Title = id,
        ActionType = actionType
    };


    /// <summary>
    /// Creates an action whose title matches its identifier.
    /// </summary>
    /// <param name="id">The action identifier (also used as the title).</param>
    /// <param name="actionType">The action behavior.</param>
    /// <param name="requiresUnlock">Requires the device to be unlocked before the action runs (see <see cref="RequiresUnlock"/>).</param>
    /// <returns>A new <see cref="ChannelAction"/>.</returns>
    public static ChannelAction Create(string id, ChannelActionType actionType, bool requiresUnlock) => new ChannelAction
    {
        Identifier = id,
        Title = id,
        ActionType = actionType,
        RequiresUnlock = requiresUnlock
    };


    /// <summary>
    /// Creates an action with a separate identifier and title.
    /// </summary>
    /// <param name="id">The action identifier.</param>
    /// <param name="title">The action button text.</param>
    /// <param name="actionType">The action behavior.</param>
    /// <returns>A new <see cref="ChannelAction"/>.</returns>
    public static ChannelAction Create(string id, string title, ChannelActionType actionType = ChannelActionType.None) => new ChannelAction
    {
        Identifier = id,
        Title = title,
        ActionType = actionType
    };


    /// <summary>
    /// Creates an action with a separate identifier and title.
    /// </summary>
    /// <param name="id">The action identifier.</param>
    /// <param name="title">The action button text.</param>
    /// <param name="actionType">The action behavior.</param>
    /// <param name="requiresUnlock">Requires the device to be unlocked before the action runs (see <see cref="RequiresUnlock"/>).</param>
    /// <returns>A new <see cref="ChannelAction"/>.</returns>
    public static ChannelAction Create(string id, string title, ChannelActionType actionType, bool requiresUnlock) => new ChannelAction
    {
        Identifier = id,
        Title = title,
        ActionType = actionType,
        RequiresUnlock = requiresUnlock
    };
}


/// <summary>
/// Describes how a notification action behaves when invoked.
/// </summary>
public enum ChannelActionType
{
    /// <summary>
    /// Shows an inline text input field so the user can reply directly from the notification.
    /// </summary>
    TextReply,

    /// <summary>
    /// Marks the action as destructive; the system styles it accordingly (e.g. red on iOS).
    /// </summary>
    Destructive,

    /// <summary>
    /// Brings the app to the foreground when the action is tapped.
    /// </summary>
    OpenApp,

    /// <summary>
    /// A plain action that fires the delegate without special styling or behavior.
    /// </summary>
    None
}
