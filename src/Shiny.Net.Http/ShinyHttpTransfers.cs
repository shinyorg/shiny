using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Net.Http;


namespace Shiny
{
    public static class ShinyHttpTransfers
    {
        static IHttpTransferManager Current { get; } = ShinyHost.Resolve<IHttpTransferManager>();

        public static Task Cancel(string id) => Current.Cancel(id);
        public static Task Cancel(QueryFilter? filter = null) => Current.Cancel(filter);
        public static Task<HttpTransfer> Enqueue(HttpTransferRequest request) => Current.Enqueue(request);
        public static Task<HttpTransfer> GetTransfer(string id) => Current.GetTransfer(id);
        public static Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter? filter = null) => Current.GetTransfers(filter);
        public static IObservable<HttpTransfer> WhenUpdated() => Current.WhenUpdated();
    }
}
