namespace Shiny.Notifications.Extensions.AI;

/// <summary>
/// Opt-in builder for the reminder (local notification) operations an AI agent is allowed to
/// perform. Nothing is exposed to the LLM unless you add it here.
/// </summary>
public interface INotificationAIToolBuilder
{
    /// <summary>
    /// Allows the AI agent to work with reminders (local notifications). Defaults to
    /// <see cref="ReminderAICapabilities.Read"/>; pass <see cref="ReminderAICapabilities.ReadWrite"/>
    /// (or <c>Write</c>) to also expose scheduling/cancelling tools.
    /// </summary>
    /// <param name="capabilities">The operations to allow.</param>
    /// <param name="channel">
    /// Optional channel id that scheduled reminders are posted to. When null the platform default
    /// channel is used. The channel must already be registered via <c>INotificationManager.AddChannel</c>.
    /// </param>
    INotificationAIToolBuilder AddReminders(
        ReminderAICapabilities capabilities = ReminderAICapabilities.Read,
        string? channel = null
    );
}
