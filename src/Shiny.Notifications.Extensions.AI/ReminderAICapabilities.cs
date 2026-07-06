namespace Shiny.Notifications.Extensions.AI;

/// <summary>
/// Operations the LLM may perform with local notifications ("reminders"). This is the allow-list
/// you present to (and control on behalf of) the AI agent - not an OS permission prompt. Read tools
/// list pending reminders; Write tools schedule and cancel them.
/// </summary>
[Flags]
public enum ReminderAICapabilities
{
    /// <summary>No reminder tools are exposed.</summary>
    None = 0,
    /// <summary>Expose read tools that let the LLM list pending/scheduled reminders.</summary>
    Read = 1 << 0,
    /// <summary>Expose write tools that let the LLM create (schedule) and cancel reminders.</summary>
    Write = 1 << 1,
    /// <summary>Expose both read and write reminder tools.</summary>
    ReadWrite = Read | Write
}
