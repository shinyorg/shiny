using Microsoft.Extensions.AI;

namespace Shiny.Contacts.Extensions.AI.Internal;

static class ContactAIFunctionFactory
{
    public static IReadOnlyList<AITool> Build(IContactStore store, ContactAICapabilities capabilities)
    {
        var tools = new List<AITool>();

        if (capabilities.HasFlag(ContactAICapabilities.Read))
        {
            tools.Add(new SearchContactsFunction(store));
            tools.Add(new GetContactFunction(store));
        }

        if (capabilities.HasFlag(ContactAICapabilities.Write))
        {
            tools.Add(new CreateContactFunction(store));
            tools.Add(new UpdateContactFunction(store));
            tools.Add(new DeleteContactFunction(store));
        }

        return tools;
    }
}
