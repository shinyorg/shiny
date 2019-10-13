using System;


namespace Shiny.Net.Http
{
    public static class CrossHttpTransfers
    {
        public static IHttpTransferManager Current => ShinyHost.Resolve<IHttpTransferManager>();
    }
}
