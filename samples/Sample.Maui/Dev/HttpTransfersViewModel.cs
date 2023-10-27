using Microsoft.Extensions.Configuration;
using Shiny.Net.Http;

namespace Sample.Dev;


public class HttpTransfersViewModel : ViewModel
{
    readonly HttpTransferMonitor monitor;
    readonly IHttpTransferManager manager;
    readonly IConfiguration configuration;


    public HttpTransfersViewModel(
        BaseServices services,
        IConfiguration configuration,
        IHttpTransferManager manager,
        HttpTransferMonitor monitor
    ) : base(services)
    {
        this.configuration = configuration;
        this.manager = manager;
        this.monitor = monitor;

        this.Clear = ReactiveCommand.Create(() =>
        {
            this.monitor.Clear(true, true, true);
        });

        this.AddDownload = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = Path.GetTempFileName();
            await this.manager.Queue(new HttpTransferRequest(
                Guid.NewGuid().ToString(),
                this.GetUrl(false),
                false,
                path
            ));
        });

        this.AddUpload = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = GenerateFile(50);

            await this.manager.Queue(new HttpTransferRequest(
                Guid.NewGuid().ToString(),
                this.GetUrl(true),
                true,
                path
            ));
        });

        this.CancelAll = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var confirm = await this.Dialogs.DisplayAlertAsync(
                    "Confirm",
                    "Are you sure you want to cancel all transfers?",
                    "Yes",
                    "No"
                );
                if (confirm)
                    await manager.CancelAll();
            },
            manager
                .WatchCount()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x > 0)
        );
    }


    [Reactive] public int TransferCount { get; private set; }
    public INotifyReadOnlyCollection<HttpTransferObject> Transfers => this.monitor.Transfers;
    public ICommand AddDownload { get; }
    public ICommand AddUpload { get; }
    public ICommand CancelAll { get; }
    public ICommand Clear { get; }


    public override async void OnAppearing()
    {
        base.OnAppearing();
        this.manager
            .WatchCount()
            .SubOnMainThread(x =>
            {
                this.TransferCount = x;
                this.Title = $"HTTP Transfers Dev ({x})";
            })
            .DisposedBy(this.DeactivateWith);

        try
        {
            await this.monitor.Start(
                false,
                false,
                false,
                RxApp.MainThreadScheduler
            );
        }
        catch (Exception ex)
        {
            await this.Dialogs.DisplayAlertAsync("Error", ex.ToString(), "OK");
        }
    }


    public override void OnDisappearing()
    {
        base.OnDisappearing();
        this.monitor.Stop();
    }


    string GetUrl(bool upload)
    {
        var url = this.configuration.GetValue("HttpTestsUrl", "https://192.168.1.5:7133")!;
        if (!url.EndsWith("/"))
            url += "/";

        url += upload ? "transfers/upload" : "transfers/download";
        return url;
    }


    static string GenerateFile(int sizeInMB)
    {
        var path = Path.GetTempFileName();

        // generate file
        var data = new byte[8192];
        var rng = new Random();
        using var fs = File.OpenWrite(path);

        for (var i = 0; i < sizeInMB * 128; i++)
        {
            rng.NextBytes(data);
            fs.Write(data, 0, data.Length);
        }
        fs.Flush();

        return path;
    }
}