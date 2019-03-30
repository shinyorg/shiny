using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Shiny.Net.Http
{
    public class HttpTransferRequest
    {
        /// <summary>
        /// </summary>
        /// <param name="uri">The URI for where to push/pull the file</param>
        /// <param name="localFilePath">
        /// For uploads, localFilePath MUST be set
        /// For downloads, localFilePath can be empty and will be given a temporary filename.  The result set will also contain the remote filename from the server.  You can move the file upon completion
        /// </param>
        public HttpTransferRequest(string uri, string localFilePath)
        {
            this.Uri = uri;
            this.LocalFilePath = new FileInfo(localFilePath);
        }


        public string Uri { get; }
        public FileInfo LocalFilePath { get; set; }
        public bool UseMeteredConnection { get; set; }
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
        public string PostData { get; set; }
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
