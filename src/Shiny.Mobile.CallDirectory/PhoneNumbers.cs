namespace Shiny.CallDirectory;


/// <summary>Converts phone numbers to the form the call directory stores.</summary>
public static class PhoneNumbers
{
    /// <summary>
    /// Strips formatting and the leading '+' or international prefix ("00"). A number with no country code
    /// (no '+', no "00") gets <paramref name="defaultCountryCode"/>, after dropping a national trunk '0'.
    /// </summary>
    /// <param name="number">Any human-written phone number, e.g. "+1 (800) 555-0100" or "800-555-0100".</param>
    /// <param name="defaultCountryCode">The country code for numbers written without one.</param>
    /// <exception cref="FormatException">The number has no digits or is too long to be an E.164 number.</exception>
    public static long ToCallDirectory(string number, int defaultCountryCode = 1)
    {
        ArgumentNullException.ThrowIfNull(number);
        var trimmed = number.Trim();
        var international = trimmed.StartsWith('+');

        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        if (!international && digits.StartsWith("00"))
        {
            international = true;
            digits = digits[2..];
        }

        if (digits.Length == 0)
            throw new FormatException($"'{number}' is not a phone number the call directory can store");

        if (!international)
        {
            var national = digits.TrimStart('0');
            var cc = defaultCountryCode.ToString();
            // "1 800 555 0100" already carries the code - E.164 numbers are at most 15 digits, a national one rarely 11+
            digits = national.StartsWith(cc) && national.Length > 10 ? national : cc + national;
        }

        if (digits.Length == 0 || digits.Length > 15)
            throw new FormatException($"'{number}' is not a phone number the call directory can store");

        return long.Parse(digits);
    }
}
