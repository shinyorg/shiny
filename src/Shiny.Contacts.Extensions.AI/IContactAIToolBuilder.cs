namespace Shiny.Contacts.Extensions.AI;

/// <summary>
/// Opt-in builder for the contact operations an AI agent is allowed to perform. Nothing is exposed
/// to the LLM unless you add it here.
/// </summary>
public interface IContactAIToolBuilder
{
    /// <summary>
    /// Allows the AI agent to work with device contacts. Defaults to
    /// <see cref="ContactAICapabilities.Read"/>; pass <see cref="ContactAICapabilities.ReadWrite"/>
    /// (or <c>Write</c>) to also expose create/update/delete tools.
    /// </summary>
    /// <param name="capabilities">The operations to allow.</param>
    IContactAIToolBuilder AddContacts(ContactAICapabilities capabilities = ContactAICapabilities.Read);
}
