using System;
using System.Windows.Input;
using Shiny.Net.Http;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Samples.HttpTransfers
{
    public class NewViewModel : ViewModel
    {
        public NewViewModel(IDownloadManager manager, INavigationService navigationService)
        {
            this.Url = "http://ipv4.download.thinkbroadband.com/1GB.zip";

            this.Save = ReactiveCommand.CreateFromTask(async () =>
            {
	            this.ErrorMessage = String.Empty;
	            try
	            {
	                if (!Uri.TryCreate(this.Url, UriKind.Absolute, out _))
	                {
	                    this.ErrorMessage = "Invalid URL";
	                }
	                else if (this.IsUpload && String.IsNullOrWhiteSpace(this.LocalFilePath))
	                {
	                    this.LocalFilePath = "You must enter the file to upload";
	                }

	                //if (this.IsUpload)
	                //{
	                //    manager.Upload(this.Url, this.LocalFilePath);
	                //}
	                //else
	                //{
	                //    manager.Download(this.Url);
	                //}

	                if (String.IsNullOrWhiteSpace(this.ErrorMessage))
	                    await navigationService.GoBackAsync();
	            }
	            catch (Exception ex)
	            {
	                this.ErrorMessage = ex.ToString();
                }
            });
        }


        public ICommand Save { get; }
        [Reactive] public string ErrorMessage { get; private set; }
        [Reactive] public string Url { get; set; }
        [Reactive] public string LocalFilePath { get; private set; }
        [Reactive] public bool IsUpload { get; set; }
    }
}
