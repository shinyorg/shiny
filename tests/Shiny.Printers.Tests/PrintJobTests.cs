using Shiny.Printing;

namespace Shiny.Printers.Tests;


public class PrintJobTests
{
    [Fact]
    public void Pdf_Rejects_Empty()
        => Assert.Throws<ArgumentException>(() => PrintJob.Pdf(ReadOnlyMemory<byte>.Empty));

    [Fact]
    public void Image_Rejects_Empty()
        => Assert.Throws<ArgumentException>(() => PrintJob.Image(ReadOnlyMemory<byte>.Empty));

    [Fact]
    public void Html_Rejects_Empty()
        => Assert.Throws<ArgumentException>(() => PrintJob.Html(""));

    [Fact]
    public void Pdf_From_Stream_Buffers_Bytes()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var job = PrintJob.Pdf(new MemoryStream(bytes));

        Assert.Equal(PrintContentKind.Pdf, job.Kind);
    }

    [Fact]
    public void Options_Default_When_Not_Supplied()
    {
        var job = PrintJob.Html("<b>hi</b>");

        Assert.NotNull(job.Options);
        Assert.False(job.Options.PreferSilent);
        Assert.Equal(1, job.Options.Copies);
    }

    [Theory]
    [InlineData("receipt.pdf", PrintContentKind.Pdf)]
    [InlineData("photo.PNG", PrintContentKind.Image)]
    [InlineData("scan.jpeg", PrintContentKind.Image)]
    [InlineData("page.html", PrintContentKind.Html)]
    public void File_Resolves_Kind_By_Extension(string path, PrintContentKind expected)
    {
        var job = PrintJob.File(path);

        Assert.Equal(PrintContentKind.File, job.Kind);
        Assert.Equal(expected, job.ResolveFileKind());
    }

    [Fact]
    public void File_With_Unknown_Extension_Throws_On_Resolve()
    {
        var job = PrintJob.File("data.xyz");

        Assert.Throws<NotSupportedException>(() => job.ResolveFileKind());
    }

    [Fact]
    public void Non_File_Kind_Resolves_To_Itself()
    {
        var job = PrintJob.Pdf(new byte[] { 1 });

        Assert.Equal(PrintContentKind.Pdf, job.ResolveFileKind());
    }
}
