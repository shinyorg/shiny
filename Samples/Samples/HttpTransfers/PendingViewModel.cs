using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Threading.Tasks;
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
                                IHttpTransferManager httpTransfers)
        {
            this.Create = navigation.NavigateCommand("NewTask");

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                await httpTransfers.GetTransfers();
            });
            this.CancelAll = ReactiveCommand.CreateFromTask(async () =>
            {
                await httpTransfers.Cancel();
                await this.Load.Execute().ToTask();
            });
            this.BindBusyCommand(this.Load);
        }


        public ICommand Create { get; }
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> CancelAll { get; }
        [Reactive] public IList<HttpTaskViewModel> Tasks { get; private set; }
    }
}
