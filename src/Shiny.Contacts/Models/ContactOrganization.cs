namespace Shiny.Contacts;

public class ContactOrganization
{
    public ContactOrganization() { }

    public ContactOrganization(string? company = null, string? title = null, string? department = null)
    {
        Company = company;
        Title = title;
        Department = department;
    }

    public string? Company { get; set; }
    public string? Title { get; set; }
    public string? Department { get; set; }

    public override string ToString() => Company ?? string.Empty;
}
