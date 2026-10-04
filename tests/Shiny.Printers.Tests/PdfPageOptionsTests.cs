using Shiny.Printing;

namespace Shiny.Printers.Tests;


public class PdfPageOptionsTests
{
    [Fact]
    public void Defaults_To_A4_Portrait()
    {
        var page = new PdfPageOptions();

        Assert.Equal(595f, page.LaidOutWidth);
        Assert.Equal(842f, page.LaidOutHeight);
        Assert.Equal(36f, page.Margin);
    }

    [Fact]
    public void Landscape_Swaps_Dimensions()
    {
        var page = PdfPageOptions.Letter with { Orientation = PrintOrientation.Landscape };

        Assert.Equal(792f, page.LaidOutWidth);
        Assert.Equal(612f, page.LaidOutHeight);
    }

    [Fact]
    public void Portrait_Is_Tall_Even_When_Sized_Wide()
    {
        var page = new PdfPageOptions { PageWidth = 792f, PageHeight = 612f, Orientation = PrintOrientation.Portrait };

        Assert.Equal(612f, page.LaidOutWidth);
        Assert.Equal(792f, page.LaidOutHeight);
    }

    [Fact]
    public void Default_Margin_Goes_Inside_Head()
    {
        var html = HtmlMarkup.WithDefaultPageMargin("<html><head><title>x</title></head><body></body></html>", 36f);

        Assert.StartsWith("<html><head><style>@page { margin: 36pt; }</style><title>", html);
    }

    [Fact]
    public void Default_Margin_Precedes_Document_Styles()
    {
        var html = HtmlMarkup.WithDefaultPageMargin("<html><head lang=\"en\"><style>@page { margin: 0.4in; }</style></head></html>", 36f);

        Assert.True(html.IndexOf("36pt", StringComparison.Ordinal) < html.IndexOf("0.4in", StringComparison.Ordinal));
    }

    [Fact]
    public void Default_Margin_Prepended_Without_Head()
        => Assert.Equal("<style>@page { margin: 18.5pt; }</style><p>hi</p>", HtmlMarkup.WithDefaultPageMargin("<p>hi</p>", 18.5f));
}
