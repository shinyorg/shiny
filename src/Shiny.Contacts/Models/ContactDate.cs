namespace Shiny.Contacts;

public class ContactDate
{
    public ContactDate() { }

    public ContactDate(DateOnly date, ContactDateType type = ContactDateType.Birthday, string? label = null)
    {
        Date = date;
        Type = type;
        Label = label;
    }

    public DateOnly Date { get; set; }
    public ContactDateType Type { get; set; } = ContactDateType.Birthday;
    public string? Label { get; set; }

    public override string ToString() => Date.ToString("d");
}
