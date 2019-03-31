using System;
using System.Windows.Input;
using System.Reactive.Linq;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;
using Shiny.Net.Http;


namespace Samples.HttpTransfers
{
    public class NewViewModel : ViewModel
    {
        public NewViewModel(IDownloadManager downloads,
                            IUploadManager uploads,
                            INavigationService navigationService)
        {
            this.WhenAnyValue(x => x.IsUpload)
                .Subscribe(upload =>
                {
                    if (upload)
                        this.Title = "New Upload";
                    else
                    {
                        this.Title = "New Download";
                        if (this.Url.IsEmpty())
                            this.Url = "http://ipv4.download.thinkbroadband.com/1GB.zip";
                    }
                });

            this.Save = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var request = new HttpTransferRequest(this.Url, this.LocalFileName)
                    {
                        UseMeteredConnection = this.UseMeteredConnection
                    };
                    if (this.IsUpload)
                        await uploads.Create(request);
                    else
                        await downloads.Create(request);

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
        [Reactive] public string ErrorMessage { get; private set; }
        [Reactive] public string Url { get; set; }
        [Reactive] public string LocalFileName { get; set; }
        [Reactive] public bool UseMeteredConnection { get; set; }
        [Reactive] public bool IsUpload { get; set; }
    }
}
