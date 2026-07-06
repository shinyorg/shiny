namespace Shiny.Contacts;

public class ContactEmail
{
    public ContactEmail() { }

    public ContactEmail(string address, EmailType type = EmailType.Home, string? label = null)
    {
        Address = address;
        Type = type;
        Label = label;
    }

    public string Address { get; set; } = string.Empty;
    public EmailType Type { get; set; } = EmailType.Home;
    public string? Label { get; set; }

    public override string ToString() => Address;
}
