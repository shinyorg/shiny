using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;
using Shiny.Net.Http;


namespace Samples.HttpTransfers
{
    public class MainViewModel : ViewModel
    {
        public MainViewModel(IUploadManager uploads,
                             IDownloadManager downloads)
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
            this.CancelAll = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.WhenAll(
                    downloads.CancelAll(),
                    uploads.CancelAll()
                );

            });
        }


        public ReactiveCommand<Unit, Unit> Load { get; }
        public ICommand NewTask { get; }
        public ICommand MoreInfo { get; }
        public ICommand CancelAll { get; }
        public ObservableCollection<HttpTaskViewModel> Tasks { get; }
    }
}
