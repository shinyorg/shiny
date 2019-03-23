using System;


namespace Shiny.Net.Http.Internals
{
    public class HttpTransferStore
    {
        public string Identifier { get; set; }
        public string RemoteFileName { get; set; }
        public string LocalFileName { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
