using System;
using System.Windows.Input;
using Shiny.Net.Http;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;

namespace Samples.HttpTransfers
{
    public class NewViewModel : ViewModel
    {
        public NewViewModel(IDownloadManager downloads,
                            IUploadManager uploads,
                            INavigationService navigationService)
        {
            this.Save = ReactiveCommand.CreateFromTask(
                async () =>
                {
                    var request = new HttpTransferRequest(this.Url, this.LocalFileName);
                    if (this.IsUpload)
                        await uploads.Create(request);
                    else
                        await downloads.Create(request);

	                await navigationService.GoBackAsync();
                },
                this.WhenAny
                (
                    x => x.Url,
                    x => x.LocalFileName,
                    (url, fn) =>
                    {
                        this.ErrorMessage = String.Empty;
                        if (!Uri.TryCreate(url.GetValue(), UriKind.Absolute, out _))
                            this.ErrorMessage = "Invalid URL";

                        else if (this.IsUpload && fn.GetValue().IsEmpty())
                            this.ErrorMessage = "You must enter the file to upload";

                        return this.ErrorMessage.IsEmpty();
                    }
                )
            );
        }


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);
            this.IsUpload = parameters.GetValue<bool>(nameof(IsUpload));
            this.Title = this.IsUpload ? "New Upload" : "New Download";
            if (!this.IsUpload)
                this.Url = "http://ipv4.download.thinkbroadband.com/1GB.zip";
        }


        public ICommand Save { get; }
        [Reactive] public string ErrorMessage { get; private set; }
        [Reactive] public string Url { get; set; }
        [Reactive] public string LocalFileName { get; set; }
        [Reactive] public bool IsUpload { get; private set; }
    }
}
