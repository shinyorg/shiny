namespace Shiny.Calendar.Extensions.AI;

/// <summary>
/// Operations the LLM may perform on the device calendar store. This is the allow-list you present
/// to (and control on behalf of) the AI agent - not an OS permission prompt. Each capability maps to a
/// distinct set of AI tools; opt in only to what the agent should be able to do.
/// </summary>
[Flags]
public enum CalendarAICapabilities
{
    /// <summary>No calendar tools are exposed.</summary>
    None = 0,

    /// <summary>Expose read tools: list calendars, search events, and read a single event.</summary>
    Read = 1 << 0,

    /// <summary>Expose the tool that lets the LLM create new events.</summary>
    Create = 1 << 1,

    /// <summary>Expose the tool that lets the LLM update existing events.</summary>
    Update = 1 << 2,

    /// <summary>Expose the tool that lets the LLM delete events.</summary>
    Delete = 1 << 3,

    /// <summary>Expose all write tools (create, update, delete).</summary>
    Write = Create | Update | Delete,

    /// <summary>Expose every read and write tool.</summary>
    All = Read | Write
}
