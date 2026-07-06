namespace Shiny.Contacts;

public class ContactWebsite
{
    public ContactWebsite() { }

    public ContactWebsite(string url, string? label = null)
    {
        Url = url;
        Label = label;
    }

    public string Url { get; set; } = string.Empty;
    public string? Label { get; set; }

    public override string ToString() => Url;
}
