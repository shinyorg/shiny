using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Navigation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny.Net.Http;


namespace Samples.HttpTransfers
{
    public class PendingViewModel : ViewModel
    {
        public PendingViewModel(INavigationService navigation,
                                IUploadManager uploads,
                                IDownloadManager downloads)
        {
            this.NewTask = navigation.NavigateCommand("NewTask");

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                await uploads.GetTransfers();
                await downloads.GetTransfers();

            });
            this.CancelAll = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.WhenAll(
                    downloads.CancelAll(),
                    uploads.CancelAll()
                );
                await this.Load.Execute().ToTask();
            });
            this.BindBusyCommand(this.Load);
        }


        public ICommand NewTask { get; }
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> CancelAll { get; }
        [Reactive] public IList<HttpTaskViewModel> Tasks { get; private set; }
    }
}
