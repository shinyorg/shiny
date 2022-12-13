using System.Threading.Tasks;

namespace Shiny.Net.Http;


public class HttpTransferManager : IHttpTransferManager
{
    public HttpTransferManager()
    {
    }


    readonly ObservableList<IHttpTransfer> transfers = new();
    public INotifyReadOnlyCollection<IHttpTransfer> Transfers => this.transfers;


    public Task<IHttpTransfer> Add(HttpTransferRequest request) => throw new System.NotImplementedException();
    public Task Remove(string identifier) => throw new System.NotImplementedException();
}