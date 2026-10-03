using Shiny.Printing;

namespace Shiny.Printers.Tests;


public class CupsPrintServiceTests
{
    sealed class FakeCupsProcessRunner : ICupsProcessRunner
    {
        readonly Func<string, IReadOnlyList<string>, ProcessResult> handler;
        public (string FileName, IReadOnlyList<string> Args)? LastCall { get; private set; }

        public FakeCupsProcessRunner(Func<string, IReadOnlyList<string>, ProcessResult> handler)
            => this.handler = handler;

        public Task<ProcessResult> Run(string fileName, IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
        {
            this.LastCall = (fileName, arguments);
            return Task.FromResult(this.handler(fileName, arguments));
        }
    }


    [Fact]
    public void BuildPrintArguments_Emits_Printer_Copies_And_Options()
    {
        var options = new PrintOptions
        {
            PrinterId = "Office_Laser",
            Copies = 3,
            JobName = "Invoice",
            Orientation = PrintOrientation.Landscape,
            Duplex = PrintDuplex.TwoSidedLongEdge,
            Color = PrintColorMode.Monochrome
        };

        var args = CupsPrintService.BuildPrintArguments(options, "/tmp/doc.pdf");

        Assert.Equal(
            ["-d", "Office_Laser", "-n", "3", "-t", "Invoice",
             "-o", "orientation-requested=4", "-o", "sides=two-sided-long-edge",
             "-o", "print-color-mode=monochrome", "--", "/tmp/doc.pdf"],
            args
        );
    }

    [Fact]
    public void BuildPrintArguments_Minimal_When_Defaults()
    {
        var args = CupsPrintService.BuildPrintArguments(new PrintOptions(), "/tmp/doc.pdf");

        Assert.Equal(["--", "/tmp/doc.pdf"], args);
    }

    [Fact]
    public void ParsePrinters_Reads_Names_And_Default()
    {
        const string output = """
            printer Office_Laser is idle.  enabled since Mon
            printer Front_Desk disabled since Tue -
            system default destination: Front_Desk
            """;

        var printers = CupsPrintService.ParsePrinters(output);

        Assert.Equal(2, printers.Count);
        Assert.Contains(printers, p => p.Id == "Office_Laser" && !p.IsDefault);
        Assert.Contains(printers, p => p.Id == "Front_Desk" && p.IsDefault);
    }

    [Fact]
    public async Task Print_Invokes_lp_And_Reports_Submitted()
    {
        var runner = new FakeCupsProcessRunner((_, _) => new ProcessResult(0, "request id is Office_Laser-42\n", ""));
        var service = new CupsPrintService(runner);

        var result = await service.Print(PrintJob.Pdf(new byte[] { 1, 2, 3 }, new() { PrinterId = "Office_Laser" }));

        Assert.Equal(PrintStatus.Submitted, result.Status);
        Assert.Equal("Office_Laser", result.PrinterId);
        Assert.Equal("lp", runner.LastCall!.Value.FileName);
        Assert.Contains("Office_Laser", runner.LastCall.Value.Args);
    }

    [Fact]
    public async Task Print_Reports_Failure_On_NonZero_Exit()
    {
        var runner = new FakeCupsProcessRunner((_, _) => new ProcessResult(1, "", "lp: unable to print"));
        var service = new CupsPrintService(runner);

        var result = await service.Print(PrintJob.Pdf(new byte[] { 1 }));

        Assert.Equal(PrintStatus.Failed, result.Status);
        Assert.Contains("unable to print", result.Error);
    }

    [Fact]
    public async Task Print_Rejects_Html()
    {
        var runner = new FakeCupsProcessRunner((_, _) => new ProcessResult(0, "", ""));
        var service = new CupsPrintService(runner);

        var result = await service.Print(PrintJob.Html("<b>hi</b>"));

        Assert.Equal(PrintStatus.Failed, result.Status);
    }

    [Fact]
    public async Task GetPrinters_Parses_lpstat_Output()
    {
        var runner = new FakeCupsProcessRunner((file, _) =>
            file == "lpstat"
                ? new ProcessResult(0, "printer Office_Laser is idle.\nsystem default destination: Office_Laser\n", "")
                : new ProcessResult(0, "", ""));
        var service = new CupsPrintService(runner);

        var printers = await service.GetPrinters();

        var printer = Assert.Single(printers);
        Assert.Equal("Office_Laser", printer.Id);
        Assert.True(printer.IsDefault);
    }
}
