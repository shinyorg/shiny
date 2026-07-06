namespace Shiny.Contacts;

public class Contact
{
    public Contact() { }

    public string? Id { get; set; }

    // Name
    public string? NamePrefix { get; set; }
    public string? GivenName { get; set; }
    public string? MiddleName { get; set; }
    public string? FamilyName { get; set; }
    public string? NameSuffix { get; set; }
    public string? Nickname { get; set; }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            if (!string.IsNullOrWhiteSpace(GivenName) && !string.IsNullOrWhiteSpace(FamilyName))
                return $"{GivenName} {FamilyName}";

            return GivenName ?? FamilyName ?? Organization?.Company ?? string.Empty;
        }
        set => displayName = value;
    }
    string? displayName;

    // Collections
    public List<ContactPhone> Phones { get; set; } = [];
    public List<ContactEmail> Emails { get; set; } = [];
    public List<ContactAddress> Addresses { get; set; } = [];
    public List<ContactDate> Dates { get; set; } = [];
    public List<ContactRelationship> Relationships { get; set; } = [];
    public List<ContactWebsite> Websites { get; set; } = [];

    // Organization
    public ContactOrganization? Organization { get; set; }

    // Other
    public string? Note { get; set; }
    public byte[]? Photo { get; set; }
    public byte[]? Thumbnail { get; set; }

    public override string ToString() => DisplayName;
}
