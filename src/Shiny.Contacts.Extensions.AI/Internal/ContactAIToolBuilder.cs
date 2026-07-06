namespace Shiny.Contacts.Extensions.AI.Internal;

sealed class ContactAIToolBuilder : IContactAIToolBuilder
{
    public ContactAICapabilities Capabilities { get; private set; }

    public IContactAIToolBuilder AddContacts(ContactAICapabilities capabilities = ContactAICapabilities.Read)
    {
        this.Capabilities |= capabilities;
        return this;
    }
}
