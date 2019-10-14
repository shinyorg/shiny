using System;
using Shiny.Net.Http;


namespace Shiny
{
    public static class CrossHttpTransfers
    {
        public static IHttpTransferManager Current => ShinyHost.Resolve<IHttpTransferManager>();
    }
}
