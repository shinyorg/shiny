using Shiny.Net.Http;
using Shiny.Notifications;

namespace Sample.HttpTransfers;


public class CreateViewModel : ViewModel
{
    const string RANDOM_FILE_NAME = "upload.random";
    IDisposable? sub;


    public CreateViewModel(
        BaseServices services,
        IFilePicker filePicker,
        INotificationManager notifications,
        IHttpTransferManager manager
    ) : base(services)
    {
        this.WhenAnyValue(x => x.IsUpload)
            .Subscribe(upload =>
            {
                if (upload)
                {
                    this.Title = "New Upload";
                    this.HttpVerb = "POST";
                    var path = this.GetRandomFilePath();
                    if (File.Exists(path))
                        this.FilePath = path;
                }
                else
                {
                    this.Title = "New Download";
                    this.HttpVerb = "GET";
                    if (this.Url.IsEmpty())
                        this.Url = "http://ipv4.download.thinkbroadband.com/1GB.zip";

                    this.FilePath = Path.Combine(this.Platform.AppData.FullName, Guid.NewGuid().ToString() + ".download");
                }
            });

        this.TestDownload = ReactiveCommand.Create(() =>
        {
            this.IsUpload = false;
            //this.Url = "https://speed.hetzner.de/10GB.bin";
            this.Url = "http://ipv4.download.thinkbroadband.com/1GB.zip";
        });

        this.SelectUpload = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select a file to upload"
                });
                if (result != null)
                    this.FilePath = result.FullPath;
            },
            this.WhenAny(
                x => x.IsUpload,
                x => x.GetValue()
            )
        );

        this.Save = this.LoadingCommand(async () =>
        {
            this.ErrorMessage = "";
            if (this.FilePath.IsEmpty())
            {
                this.ErrorMessage = "Enter a filename";
                return;
            }
            if (!Uri.TryCreate(this.Url, UriKind.Absolute, out var uri))
            {
                this.ErrorMessage = "Please enter a valid URI";
                return;
            }
            if (this.IsUpload && !File.Exists(this.FilePath))
            {
                await this.Alert("This file does not exist");
                return;
            }

            var access = await notifications.RequestAccess();
            if (access != AccessState.Available)
                await this.Dialogs.DisplayAlertAsync("Warning", "You will not get notifications for transfers due to insufficient permissions", "OK");

            TransferHttpContent? content = null;
            if (!this.PostData.IsEmpty())
                content = new TransferHttpContent(this.PostData!);

            var request = new HttpTransferRequest
            (
                Guid.NewGuid().ToString(),
                this.Url,
                this.IsUpload,
                this.FilePath!,
                this.UseMeteredConnection,
                content
            )
            {
                HttpMethod = this.HttpVerb
            };
            await manager.Queue(request);

            await this.Navigation.GoBack();
        });

        this.CreateRandom = ReactiveCommand.CreateFromTask(
            async () =>
            {                
                if (this.SizeInMegabytes <= 0)
                {
                    await this.Alert("Invalid File Size");
                    return;
                }
                this.IsBusy = true;
                await this.GenerateRandom();
                this.FilePath = this.GetRandomFilePath();
                this.IsBusy = false;
            },
            this.WhenAny(
                x => x.IsUpload,
                x => x.GetValue()
            )
        );
    }



    public ICommand TestDownload { get; }
    public ICommand Save { get; }
    public ICommand SelectUpload { get; }
    public ICommand Delete { get; }
    public ICommand CreateRandom { get; }


    [Reactive] public string PostData { get; set; }
    [Reactive] public string HttpVerb { get; set; }
    [Reactive] public string ErrorMessage { get; private set; }
    [Reactive] public string Url { get; set; }
    [Reactive] public bool UseMeteredConnection { get; set; }
    [Reactive] public bool IsUpload { get; set; }
    [Reactive] public string FilePath { get; set; }
    [Reactive] public int SizeInMegabytes { get; set; } = 100;


    string GetRandomFilePath() => Path.Combine(this.Platform.AppData.FullName, RANDOM_FILE_NAME);
    Task GenerateRandom() => Task.Run(() =>
    {
        var path = this.GetRandomFilePath();
        if (File.Exists(path))
            File.Delete(path); // delete previous random file

        var byteSize = this.SizeInMegabytes * 1024 * 1024;
        var data = new byte[8192];
        var rng = new Random();

        using var fs = new FileStream(path, FileMode.Create);
        while (fs.Length < byteSize)
        {
            rng.NextBytes(data);
            fs.Write(data, 0, data.Length);
            fs.Flush();
        }
    });
}
