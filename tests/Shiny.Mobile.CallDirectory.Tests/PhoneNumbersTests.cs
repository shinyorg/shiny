namespace Shiny.CallDirectory.Tests;


public class PhoneNumbersTests
{
    [Theory]
    [InlineData("+1 (800) 555-0100", 1, 18005550100)]
    [InlineData("800-555-0100", 1, 18005550100)]
    [InlineData("1 800 555 0100", 1, 18005550100)]
    [InlineData("+44 20 7946 0958", 1, 442079460958)]
    [InlineData("0044 20 7946 0958", 1, 442079460958)]
    [InlineData("020 7946 0958", 44, 442079460958)]
    public void Converts(string input, int defaultCountryCode, long expected)
        => Assert.Equal(expected, PhoneNumbers.ToCallDirectory(input, defaultCountryCode));


    [Theory]
    [InlineData("")]
    [InlineData("call me")]
    [InlineData("+1234567890123456")]
    public void RejectsNonNumbers(string input)
        => Assert.Throws<FormatException>(() => PhoneNumbers.ToCallDirectory(input));
}
