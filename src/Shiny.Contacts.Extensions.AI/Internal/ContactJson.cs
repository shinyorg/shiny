using System.Text.Json.Nodes;

namespace Shiny.Contacts.Extensions.AI.Internal;

/// <summary>Projects <see cref="Contact"/> instances into LLM-friendly JSON (no photos/binary).</summary>
static class ContactJson
{
    public static JsonObject Summary(Contact c) => new()
    {
        ["id"] = c.Id,
        ["displayName"] = c.DisplayName,
        ["phones"] = Phones(c),
        ["emails"] = Emails(c)
    };

    public static JsonObject Detail(Contact c)
    {
        var o = Summary(c);
        o["givenName"] = c.GivenName;
        o["middleName"] = c.MiddleName;
        o["familyName"] = c.FamilyName;
        o["nickname"] = c.Nickname;
        o["note"] = c.Note;

        if (c.Organization != null)
            o["organization"] = new JsonObject
            {
                ["company"] = c.Organization.Company,
                ["title"] = c.Organization.Title,
                ["department"] = c.Organization.Department
            };

        var addresses = new JsonArray();
        foreach (var a in c.Addresses)
            addresses.Add((JsonNode)new JsonObject
            {
                ["type"] = a.Type.ToString(),
                ["street"] = a.Street,
                ["city"] = a.City,
                ["state"] = a.State,
                ["postalCode"] = a.PostalCode,
                ["country"] = a.Country
            });
        o["addresses"] = addresses;

        return o;
    }

    static JsonArray Phones(Contact c)
    {
        var arr = new JsonArray();
        foreach (var p in c.Phones)
            arr.Add((JsonNode)new JsonObject { ["number"] = p.Number, ["type"] = p.Type.ToString() });
        return arr;
    }

    static JsonArray Emails(Contact c)
    {
        var arr = new JsonArray();
        foreach (var e in c.Emails)
            arr.Add((JsonNode)new JsonObject { ["address"] = e.Address, ["type"] = e.Type.ToString() });
        return arr;
    }
}
