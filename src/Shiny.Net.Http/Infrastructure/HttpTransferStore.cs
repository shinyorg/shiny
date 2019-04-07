using System;
using System.Collections.Generic;


namespace Shiny.Net.Http.Infrastructure
{
    public class HttpTransferStore
    {
        public string Identifier { get; set; }
        public string LocalFilePath { get; set; }
        public bool IsUpload { get; set; }
        public string Description { get; set; }
        public string Uri { get; set; }
        public bool UseMeteredConnection { get; set; }
        public string HttpMethod { get; set; }
        public string PostData { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }
}
