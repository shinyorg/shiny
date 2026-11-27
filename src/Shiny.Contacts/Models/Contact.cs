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

            var hasGiven = !string.IsNullOrWhiteSpace(GivenName);
            var hasFamily = !string.IsNullOrWhiteSpace(FamilyName);
            if (hasGiven && hasFamily)
                return $"{GivenName} {FamilyName}";

            // platforms hand back empty strings (not null) for unset names, so test for content
            if (hasGiven)
                return GivenName!;

            if (hasFamily)
                return FamilyName!;

            return string.IsNullOrWhiteSpace(Organization?.Company) ? string.Empty : Organization!.Company!;
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
