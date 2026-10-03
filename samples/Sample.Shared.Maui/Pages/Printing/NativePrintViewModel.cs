using Shiny.Printers;
using Shiny.Printing;
using Shiny.Printing.Rendering;

namespace Sample.Shared.Maui.Pages.Printing;


[ShellMap<NativePrintPage>("nativeprint")]
public partial class NativePrintViewModel(
    IPrintService printService,
    IPrintDocumentRenderer renderer
) : ObservableObject
{
    const string Html = """
        <html><body style="font-family:sans-serif">
        <h1>Shiny Native Print</h1>
        <p>This HTML was rendered by the platform print pipeline.</p>
        </body></html>
        """;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintPdfCommand))]
    [NotifyCanExecuteChangedFor(nameof(PrintHtmlCommand))]
    [NotifyCanExecuteChangedFor(nameof(ListPrintersCommand))]
    bool isBusy;

    [ObservableProperty] string status = "Choose something to print";
    [ObservableProperty] bool silent;

    public string CapabilitiesText => $"This platform supports: {printService.Capabilities}";
    public bool SupportsHtml => printService.Capabilities.HasFlag(PrintingCapabilities.Html);
    public bool SupportsSilent => printService.Capabilities.HasFlag(PrintingCapabilities.Silent);
    public bool SupportsEnumerate => printService.Capabilities.HasFlag(PrintingCapabilities.EnumeratePrinters);

    public List<PrinterInfo> Printers
    {
        get;
        private set
        {
            field = value;
            this.OnPropertyChanged();
        }
    } = [];

    [ObservableProperty] PrinterInfo? selectedPrinter;


    // the same receipt the thermal page streams as ESC/POS, rendered to a PDF any printer can take
    [RelayCommand(CanExecute = nameof(NotBusy))]
    Task PrintPdf()
    {
        var pdf = renderer.RenderToPdf(SampleReceipt.Build(PrinterCapabilities.Paper80mm), PrintRenderOptions.Letter);
        return this.Submit(PrintJob.Pdf(pdf, this.Options("Shiny Receipt")));
    }


    [RelayCommand(CanExecute = nameof(NotBusy))]
    Task PrintHtml() => this.Submit(PrintJob.Html(Html, this.Options("Shiny HTML")));


    [RelayCommand(CanExecute = nameof(NotBusy))]
    async Task ListPrinters()
    {
        this.IsBusy = true;
        try
        {
            this.Printers = (await printService.GetPrinters()).ToList();
            this.SelectedPrinter = this.Printers.FirstOrDefault(x => x.IsDefault) ?? this.Printers.FirstOrDefault();
            this.Status = this.Printers.Count == 0 ? "No printers found" : $"{this.Printers.Count} printer(s)";
        }
        finally
        {
            this.IsBusy = false;
        }
    }

    bool NotBusy() => !this.IsBusy;


    // silent printing needs a target; without one the OS dialog picks it
    PrintOptions Options(string jobName) => new()
    {
        JobName = jobName,
        PreferSilent = this.Silent && this.SelectedPrinter != null,
        PrinterId = this.SelectedPrinter?.Id
    };


    async Task Submit(PrintJob job)
    {
        this.IsBusy = true;
        this.Status = "Printing...";
        try
        {
            var result = await printService.Print(job);
            this.Status = result.IsSuccess
                ? $"{result.Status}{(result.PrinterId is { } id ? $" → {id}" : "")}"
                : $"{result.Status}: {result.Error}";
        }
        catch (Exception ex)
        {
            this.Status = $"Error: {ex.Message}";
        }
        finally
        {
            this.IsBusy = false;
        }
    }
}
