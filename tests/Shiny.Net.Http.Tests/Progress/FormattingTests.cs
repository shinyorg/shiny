using Xunit;

namespace Shiny.Net.Http.Tests;


public class FormattingTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(999, "999 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10 * 1024, "10 KB")]
    [InlineData(13_002_342, "12 MB")]
    [InlineData(1_610_612_736, "1.5 GB")]
    public void FormatBytes(long bytes, string expected)
        => Assert.Equal(expected, TransferProgressContentBuilder.FormatBytes(bytes));


    [Fact]
    public void FormatBytes_NegativeClampsToZero()
        => Assert.Equal("0 B", TransferProgressContentBuilder.FormatBytes(-5));


    [Fact]
    public void FormatRate_AppendsPerSecond()
        => Assert.Equal("1.5 MB/s", TransferProgressContentBuilder.FormatRate(1_572_864));


    [Theory]
    [InlineData(0.0, "0%")]
    [InlineData(0.414, "41%")]
    [InlineData(1.0, "100%")]
    [InlineData(1.7, "100%")]
    [InlineData(-0.2, "0%")]
    public void FormatPercent(double fraction, string expected)
        => Assert.Equal(expected, TransferProgressContentBuilder.FormatPercent(fraction));


    [Theory]
    [InlineData(0.4, false, "<1s")]
    [InlineData(45, false, "45s")]
    [InlineData(252, false, "4m 12s")]
    [InlineData(252, true, "4m")]
    [InlineData(120, false, "2m")]
    [InlineData(4800, false, "1h 20m")]
    [InlineData(4800, true, "1h")]
    public void FormatDuration(double seconds, bool abbreviated, string expected)
        => Assert.Equal(expected, TransferProgressContentBuilder.FormatDuration(TimeSpan.FromSeconds(seconds), abbreviated));


    [Fact]
    public void Formatting_IsCultureInvariant()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            // de-DE uses a comma as the decimal separator - the widget and any server push must not see one
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            Assert.Equal("1.5 KB", TransferProgressContentBuilder.FormatBytes(1536));

            var data = TransferProgressContentBuilder.BuildData(
                TestData.Snapshot(TestData.Result(transferred: 41, total: 100))
            );
            Assert.Equal("0.41", data["percent"]);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }
}
