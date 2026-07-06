namespace Shiny.Contacts;

public class ContactAddress
{
    public ContactAddress() { }

    public ContactAddress(
        string? street = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        string? country = null,
        AddressType type = AddressType.Home,
        string? label = null)
    {
        Street = street;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        Type = type;
        Label = label;
    }

    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public AddressType Type { get; set; } = AddressType.Home;
    public string? Label { get; set; }

    public override string ToString()
    {
        var parts = new[] { Street, City, State, PostalCode, Country }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }
}
