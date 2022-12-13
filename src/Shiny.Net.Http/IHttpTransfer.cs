using System.ComponentModel;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransfer : INotifyPropertyChanged
{
    HttpTransferRequest Request { get; }

    Task Pause();  // not for upload on ios
    Task Resume(); // not for upload on ios
    Task Cancel();

    HttpTransferState Status { get; }
    string Identifier { get; }
    long BytesTransferred { get; }
    long BytesToTransfer { get; }
}