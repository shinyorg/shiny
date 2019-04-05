using System;


namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransfer : IHttpTransfer
    {
        protected AbstractHttpTransfer(HttpTransferRequest request, string id)
        {
            this.Identifier = id;
            this.Request = request;
            if (request.IsUpload)
            {
                this.FileSize = request.LocalFile.Length;
                this.RemoteFileName = request.LocalFile.Name;
            }
        }


        public HttpTransferRequest Request { get; }
        public string Identifier { get; }
        public Exception Exception { get; protected internal set; }
        public HttpTransferState Status { get; protected internal set; }
        public string RemoteFileName { get; protected internal set; }
        public long FileSize { get; protected internal set; }
        public long BytesTransferred { get; protected internal set; }
        public DateTime LastModified { get; protected internal set; }
    }
}
