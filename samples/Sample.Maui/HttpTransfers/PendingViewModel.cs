using System.Reactive.Disposables;
using Shiny.Net.Http;

namespace Sample.HttpTransfers;


public class PendingViewModel : ViewModel
{
    public PendingViewModel(BaseServices services, IHttpTransferManager httpTransfers) : base(services)
    {
        this.Create = this.Navigation.Command("HttpTransfersCreate");
        this.Load = this.LoadingCommand(async () =>
        {            
            this.Transfers = httpTransfers
                .Transfers
                .Select(transfer => new HttpTransferViewModel(
                    transfer,
                    this.DestroyWith,
                    this.ConfirmCommand(
                        "Are you sure you want to resume this transfer?",
                        () => httpTransfers.Resume(transfer.Identifier)
                    ),
                    this.ConfirmCommand(
                        "Are you sure you wish to pause this transfer?",
                        () => httpTransfers.Pause(transfer.Identifier)
                    ),
                    this.ConfirmCommand(
                        "Are you sure you want to cancel this transfer?",
                        () => httpTransfers.Cancel(transfer.Identifier)
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


    public ICommand Create { get; }
    public ICommand Load { get; }
    public ICommand CancelAll { get; }


    IList<HttpTransferViewModel> transfers = null!;
    public IList<HttpTransferViewModel> Transfers
    {
        get => this.transfers;
        private set
        {
            this.transfers = value;
            this.RaisePropertyChanged();
        }
    }

    public override void OnAppearing() => this.Load.Execute(null);
}



public class HttpTransferViewModel : ReactiveObject
{
    readonly IHttpTransfer transfer;


    public HttpTransferViewModel(
        IHttpTransfer transfer,
        CompositeDisposable disposer,
        ICommand resume,
        ICommand pause,
        ICommand cancel
    )
    {
        this.transfer = transfer;
        this.Resume = resume;
        this.Pause = pause;
        this.Cancel = cancel;

        this.transfer
            .ListenToMetrics()
            .SubOnMainThread(x =>
            {
                this.TransferSpeed = Math.Round((decimal)x.BytesPerSecond / 1024, 2) + "Kb/s";
                this.EstimateTimeRemaining = Math.Round(x.EstimatedTimeRemaining.TotalMinutes, 1) + " min(s)";
                this.PercentComplete = x.PercentComplete;
                this.Status = x.Status.ToString();
            })
            .DisposedBy(disposer);
    }

    public ICommand Resume { get; }
    public ICommand Pause { get; }
    public ICommand Cancel { get; }

    public string Identifier => this.transfer.Identifier;
    public bool IsUpload => this.transfer.Request.IsUpload;
    public string Uri => this.transfer.Request.Uri;

    [Reactive] public double PercentComplete { get; private set; }
    [Reactive] public string Status { get; private set; } = null!;
    [Reactive] public string TransferSpeed { get; private set; } = null!;
    [Reactive] public string EstimateTimeRemaining { get; private set; } = null!;
}
