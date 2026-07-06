using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Shiny.Contacts.Extensions.AI.Internal;

/// <summary>Searches device contacts by a free-text query across name, phone, and email.</summary>
sealed class SearchContactsFunction : ContactAIFunctionBase
{
    const int DefaultLimit = 20;

    public SearchContactsFunction(IContactStore store)
        : base(store, "search_contacts",
            "Search the device's contacts by a free-text query matched against display name, given/family name, phone numbers, and email addresses. Returns matching contacts (id, name, phones, emails). Use the id with other contact tools.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["query"] = AiSchema.String("Text to match against name, phone number, or email. Leave empty to list all contacts."),
                ["limit"] = AiSchema.Integer($"Maximum number of contacts to return (default {DefaultLimit}).")
            }));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var query = GetString(arguments, "query")?.Trim();
        var limit = GetInt(arguments, "limit") ?? DefaultLimit;
        if (limit < 1)
            limit = DefaultLimit;

        var all = await this.Store.GetAll(cancellationToken).ConfigureAwait(false);

        IEnumerable<Contact> matches = all;
        if (!string.IsNullOrWhiteSpace(query))
        {
            matches = all.Where(c => Matches(c, query));
        }

        var results = new JsonArray();
        foreach (var c in matches.Take(limit))
            results.Add((JsonNode)ContactJson.Summary(c));

        return new JsonObject { ["count"] = results.Count, ["contacts"] = results };
    }

    static bool Matches(Contact c, string query)
    {
        bool Has(string? s) => s != null && s.Contains(query, StringComparison.OrdinalIgnoreCase);

        return Has(c.DisplayName)
            || Has(c.GivenName)
            || Has(c.FamilyName)
            || Has(c.Nickname)
            || Has(c.Organization?.Company)
            || c.Phones.Any(p => Has(p.Number))
            || c.Emails.Any(e => Has(e.Address));
    }
}

/// <summary>Reads a single contact by its platform identifier, including full detail.</summary>
sealed class GetContactFunction : ContactAIFunctionBase
{
    public GetContactFunction(IContactStore store)
        : base(store, "get_contact",
            "Read a single contact by its platform id (as returned by search_contacts), including phones, emails, addresses, organization, and note.",
            BuildSchema())
    { }

    static JsonElement BuildSchema()
        => AiSchema.ToElement(AiSchema.Object(
            new JsonObject
            {
                ["contactId"] = AiSchema.String("The platform contact id.")
            },
            "contactId"));

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var id = GetString(arguments, "contactId");
        if (string.IsNullOrWhiteSpace(id))
            return new JsonObject { ["error"] = "'contactId' is required." };

        var c = await this.Store.GetById(id, cancellationToken).ConfigureAwait(false);
        if (c is null)
            return new JsonObject { ["error"] = $"No contact found with id '{id}'." };

        return ContactJson.Detail(c);
    }
}
