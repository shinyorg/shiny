using System;


namespace Shiny.Net.Http
{
    public class HttpTransferMetrics
    {

        internal DateTime? LastChanged { get; set; }
        internal long LastBytesTransferred { get; set; }
        //internal HttpTransferState PreviousStatus { get; set; }
    }
}
