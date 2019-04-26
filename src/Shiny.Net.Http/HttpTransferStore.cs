using System;
using System.Collections.Generic;


namespace Shiny.Net.Http
{
    public class HttpTransferStore
    {
        public string Id { get; set; }
        public bool IsUpload { get; set; }
        public string Uri { get; set; }
        public string LocalFile { get; set; }
        public bool UseMeteredConnection { get; set; }
        public string HttpMethod { get; set; }
        public string PostData { get; set; }
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
