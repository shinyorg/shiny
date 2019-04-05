using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
        readonly IHttpTransferManager httpTransfers;


        public PendingViewModel(INavigationService navigation,
                                IHttpTransferManager httpTransfers)
        {
            this.Create = navigation.NavigateCommand("CreateTransfer");

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var transfers = await httpTransfers.GetTransfers();
                this.Transfers = transfers
                    .Select(transfer =>
                    {
                        var vm = new HttpTransferViewModel
                        {
                            Identifier = transfer.Identifier,
                            Uri = transfer.Request.Uri,
                            IsUpload = transfer.Request.IsUpload,

                            Cancel = ReactiveCommand.CreateFromTask(async () =>
                            {
                                await this.httpTransfers.Cancel(transfer.Identifier);
                                await this.Load.Execute();
                            })
                        };

                        ToViewModel(vm, transfer);
                        return vm;
                    })
                    .ToList();
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
        [Reactive] public IList<HttpTransferViewModel> Transfers { get; private set; }


        public override async void OnAppearing()
        {
            base.OnAppearing();
            await this.Load.Execute().ToTask();

            this.httpTransfers
                .WhenUpdated()
                .Buffer(TimeSpan.FromSeconds(1))
                .Where(x => x.Count > 0)
                .Synchronize()
                .Subscribe(this.Process)
                .DisposeWith(this.DeactivateWith);
        }


        void Process(IEnumerable<IHttpTransfer> transfers)
        {
            foreach (var transfer in transfers)
            {
                var vm = this.Transfers.FirstOrDefault(x => x.Identifier == transfer.Identifier);
                if (vm != null)
                    ToViewModel(vm, transfer);
            }
        }

        static void ToViewModel(HttpTransferViewModel viewModel, IHttpTransfer transfer)
        {
            // => Math.Round(this.transfer.BytesPerSecond.Bytes().Kilobytes, 2) + " Kb/s";
            //public string EstimateMinsRemaining => Math.Round(this.transfer.EstimatedCompletionTime.TotalMinutes, 1) + " min(s)";
            //viewModel.EstimateMinsRemaining =
            //viewModel.PercentComplete
            viewModel.Status = transfer.Status.ToString();
        }
    }
}
