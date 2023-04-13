using System.Reactive.Disposables;
using Shiny.Net.Http;

namespace Sample.HttpTransfers;


public class PendingViewModel : ViewModel
{
    readonly IHttpTransferManager httpTransfers;


    public PendingViewModel(BaseServices services, IHttpTransferManager httpTransfers) : base(services)
    {
        this.httpTransfers = httpTransfers;

        this.Create = this.Navigation.Command("HttpTransfersCreate");
        this.Load = this.LoadingCommand(async () =>
        {
            var transfers = await httpTransfers.GetTransfers();
            this.Transfers = (await httpTransfers.GetTransfers())
                .Select(transfer => new HttpTransferViewModel(
                    transfer,
                    this.ConfirmCommand(
                        "Are you sure you want to cancel this transfer?",
                        async () =>
                        {
                            await httpTransfers.Cancel(transfer.Identifier);
                            this.Load.Execute(null);
                        }
                    )
                ))
                .ToList();
        });

        this.CancelAll = this.LoadingCommand(async () =>
        {
            await httpTransfers.CancelAll();
            this.Load.Execute(null);
        });
    }


    public override void OnAppearing()
    {
        this.Load.Execute(null);
        this.httpTransfers
            .WhenUpdateReceived()
            .Select(x => (x, this.Transfers.FirstOrDefault(x => x.Identifier.Equals(x.Identifier))))
            .Where(x => x.Item2 != null)
            .SubOnMainThread(x =>
            {
                var transfer = x.Item1;
                var vm = x.Item2!;

                vm.TransferSpeed = Math.Round((decimal)transfer.Progress.BytesPerSecond / 1024, 2) + "Kb/s";
                vm.Status = transfer.Status.ToString();

                if (transfer.IsDeterministic)
                {
                    vm.EstimateTimeRemaining = Math.Round(transfer.Progress.EstimatedTimeRemaining.TotalMinutes, 1) + " min(s)";
                    vm.PercentComplete = transfer.Progress.PercentComplete;
                }
            })
            .DisposedBy(this.DeactivateWith);
    }

    public ICommand Create { get; }
    public ICommand Load { get; }
    public ICommand CancelAll { get; }

    [Reactive] public IList<HttpTransferViewModel> Transfers { get; private set; }
}


public class HttpTransferViewModel : ReactiveObject
{
    public HttpTransferViewModel(HttpTransfer transfer, ICommand cancel)
    {
        this.Identifier = transfer.Identifier;
        this.Uri = transfer.Request.Uri;
        this.IsUpload = transfer.Request.IsUpload;
        this.Cancel = cancel;
    }


    public string Identifier { get;  }
    public string Uri { get; }
    public bool IsUpload { get; }
    public ICommand Cancel { get; }

    [Reactive] public double PercentComplete { get; set; }
    [Reactive] public string Status { get; set; } = null!;
    [Reactive] public string TransferSpeed { get; set; } = null!;
    [Reactive] public string EstimateTimeRemaining { get; set; } = null!;
}