namespace Shiny.CallDirectory.Infrastructure;


/// <summary>What happens to one incoming call on Android.</summary>
enum CallScreeningAction
{
    Allow,
    Block,
    Identify
}


/// <summary>
/// The platform-free half of Android call screening - which entry an incoming handle maps to and what to do with
/// the call - so it is unit tested on net10.0 rather than only on a phone.
/// </summary>
static class CallScreening
{
    /// <summary>
    /// The stored form of an incoming number (tel: handle scheme-specific part), or null for a withheld or
    /// unparseable one.
    /// </summary>
    public static long? Normalize(string? number, int defaultCountryCode)
    {
        if (String.IsNullOrWhiteSpace(number))
            return null;

        try
        {
            return PhoneNumbers.ToCallDirectory(number, defaultCountryCode);
        }
        catch (FormatException)
        {
            return null;
        }
    }


    public static CallScreeningAction Decide(CallDirectoryEntry? entry, bool blockUnlisted) => entry?.Type switch
    {
        CallDirectoryEntryType.Block => CallScreeningAction.Block,
        // a label is all identification adds - with none there is nothing to show
        CallDirectoryEntryType.Identify => String.IsNullOrWhiteSpace(entry.Label) ? CallScreeningAction.Allow : CallScreeningAction.Identify,
        CallDirectoryEntryType.Allow => CallScreeningAction.Allow,
        _ => blockUnlisted ? CallScreeningAction.Block : CallScreeningAction.Allow
    };
}
