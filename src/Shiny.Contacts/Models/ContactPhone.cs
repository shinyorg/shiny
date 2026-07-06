namespace Shiny.Contacts;

public class ContactPhone
{
    public ContactPhone() { }

    public ContactPhone(string number, PhoneType type = PhoneType.Mobile, string? label = null)
    {
        Number = number;
        Type = type;
        Label = label;
    }

    public string Number { get; set; } = string.Empty;
    public PhoneType Type { get; set; } = PhoneType.Mobile;
    public string? Label { get; set; }

    public override string ToString() => Number;
}
