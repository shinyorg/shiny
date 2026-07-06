namespace Shiny.Contacts.Extensions.AI;

/// <summary>
/// Operations the LLM may perform on the device contact store. This is the allow-list you present
/// to (and control on behalf of) the AI agent - not an OS permission prompt. Read tools query
/// contacts; Write tools create/update/delete them.
/// </summary>
[Flags]
public enum ContactAICapabilities
{
    /// <summary>No contact tools are exposed.</summary>
    None = 0,
    /// <summary>Expose read tools that let the LLM search and look up contacts.</summary>
    Read = 1 << 0,
    /// <summary>Expose write tools that let the LLM create, update, and delete contacts.</summary>
    Write = 1 << 1,
    /// <summary>Expose both read and write contact tools.</summary>
    ReadWrite = Read | Write
}
