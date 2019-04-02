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
        /// <param name="localFilePath">The file to upload or where to move file to upon download completion</param>
        /// <param name="isUpload">Is this an upload operation</param>
        public HttpTransferRequest(string uri, string localFilePath, bool isUpload = false) : this(uri, new FileInfo(localFilePath), isUpload) { }
        public HttpTransferRequest(string uri, FileInfo localFile, bool isUpload = false)
        {
            this.Uri = uri;
            this.LocalFile = localFile;
        }


        public bool IsUpload { get; }
        public string Uri { get; }
        public FileInfo LocalFile { get; }
        public bool UseMeteredConnection { get; set; }
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
        public string PostData { get; set; }
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
