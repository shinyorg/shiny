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

    /// <summary>
    /// False for a date saved without a year (e.g. a birthday with only a day and month). Such dates carry year 1
    /// (year 4 for Feb 29) in <see cref="Date"/>; set that year to create one.
    /// </summary>
    public bool HasYear => !Internals.ContactDates.IsYearless(this.Date);

    public override string ToString() => this.HasYear ? this.Date.ToString("d") : this.Date.ToString("M");
}
