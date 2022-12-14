using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferManager
{
    INotifyReadOnlyCollection<IHttpTransfer> Transfers { get; }
    
    Task<IHttpTransfer> Queue(HttpTransferRequest request);
    Task Cancel(string identifier);
    Task Pause(string identifier);
    Task Resume(string identifier);
    Task CancelAll();
}
