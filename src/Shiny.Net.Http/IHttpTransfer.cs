using System.ComponentModel;

namespace Shiny.Net.Http;


public interface IHttpTransfer : INotifyPropertyChanged
{
    HttpTransferRequest Request { get; }

    HttpTransferState Status { get; }
    string Identifier { get; }
    long BytesTransferred { get; }
    long BytesToTransfer { get; }

    double PercentComplete { get; }
    //IObservable<object> WatchMetrics();
}