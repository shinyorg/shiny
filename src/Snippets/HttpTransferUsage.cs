using System.Threading.Tasks;
using Shiny;
using Shiny.Net.Http;

public class HttpTransferUsage
{
    public async Task Usage()
    {
        var manager = ShinyHost.Resolve<IHttpTransferManager>();
        var yourTransfer = await manager.GetTransfer("yourid");

        var yourTransfers = await manager.GetTransfers();

        //manager.WhenUpdated().Subscribe(transfer =>
        //{
        //});
    }
}