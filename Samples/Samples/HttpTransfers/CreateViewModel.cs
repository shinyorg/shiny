using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Reactive.Linq;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.IO;
using Shiny.Net.Http;
using Samples.Settings;


namespace Samples.HttpTransfers
{
    public class CreateViewModel : ViewModel
    {
        readonly IFileSystem fileSystem;


        public CreateViewModel(INavigationService navigationService,
                               IHttpTransferManager httpTransfers,
                               IFileSystem fileSystem,
                               AppSettings appSettings)
        {
            this.fileSystem = fileSystem;
            this.Url = appSettings.LastTransferUrl;
            this.LocalFileName = Guid.NewGuid().ToString();

            this.WhenAnyValue(x => x.IsUpload)
                .Subscribe(upload =>
                    this.Title = upload ? "New Upload" : "New Download"
                );

            this.ResetUrl = ReactiveCommand.Create(() =>
            {
                appSettings.LastTransferUrl = null;
                this.Url = appSettings.LastTransferUrl;
            });

            this.Save = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    if (this.IsUpload)
                        await this.GenerateUpload();

                    var request = new HttpTransferRequest(this.Url, this.GetPath(), this.IsUpload)
                    {
                        UseMeteredConnection = this.UseMeteredConnection
                    };
                    await httpTransfers.Enqueue(request);
                    appSettings.LastTransferUrl = this.Url;
	                await navigationService.GoBackAsync();
                },
                this.WhenAny
                (
                    x => x.IsUpload,
                    x => x.Url,
                    x => x.LocalFileName,
                    (upload, url, fn) =>
                    {
                        this.ErrorMessage = String.Empty;
                        if (!Uri.TryCreate(url.GetValue(), UriKind.Absolute, out _))
                            this.ErrorMessage = "Invalid URL";

                        else if (upload.GetValue() && fn.GetValue().IsEmpty())
                            this.ErrorMessage = "You must enter the file to upload";

                        return this.ErrorMessage.IsEmpty();
                    }
                )
            );
        }


        public ICommand Save { get; }
        public ICommand ResetUrl { get; }
        [Reactive] public string ErrorMessage { get; private set; }
        [Reactive] public string Url { get; set; }
        [Reactive] public string LocalFileName { get; set; }
        [Reactive] public bool UseMeteredConnection { get; set; }
        [Reactive] public bool IsUpload { get; set; }
        [Reactive] public int SizeInMegabytes { get; set; } = 100;


        Task GenerateUpload() => Task.Run(() =>
        {
            var byteSize = this.SizeInMegabytes * 1024 * 1024;
            var path = this.GetPath();
            var data = new byte[8192];
            var rng = new Random();

            using (var fs = File.OpenWrite(path))
            {
                while (fs.Length < byteSize)
                {
                    rng.NextBytes(data);
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        });


        string GetPath() => Path.Combine(this.fileSystem.AppData.FullName, this.LocalFileName);
    }
}
