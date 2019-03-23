using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Shiny.Net.Http;
using Xamarin.Forms;


namespace Samples.HttpTransfers
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(IUploadManager uploads)
        {
            //this.NewTask = new Command(async () =>
                //await App.Current.MainPage.Navigation.PushAsync(new NewTaskPage())
            //);
            //this.MoreInfo = new Command<HttpTaskViewModel>(x => x.MoreInfo.Execute(null));
            //this.CancelAll = new Command(manager.CancelAll);
            //this.Tasks = new ObservableCollection<HttpTaskViewModel>();

            //manager.CurrentTransfersChanged += (sender, args) =>
            //{
            //    if (args.Change == HttpTransferListChange.Add)
            //        Device.BeginInvokeOnMainThread(() => this.Tasks.Add(new HttpTaskViewModel(args.Task)));
            //};
        }


        public ICommand NewTask { get; }
        public ICommand MoreInfo { get; }
        public ICommand CancelAll { get; }
        public ObservableCollection<HttpTaskViewModel> Tasks { get; }
    }
}
