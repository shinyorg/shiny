using System;


namespace Shiny.Net.Http.Infrastructure
{
    public abstract class AbstractHttpTransfer : IHttpTransfer
    {
        protected AbstractHttpTransfer(HttpTransferRequest request, string id)
        {
            this.Identifier = id;
            this.Request = request;
            this.Status = HttpTransferState.Pending;

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


        public virtual double PercentComplete
        {
            get
            {
                if (this.BytesTransferred <= 0 || this.FileSize <= 0)
                    return 0;

                var raw = ((double)this.BytesTransferred / (double)this.FileSize);
                return Math.Round(raw, 2);
            }
        }

    }
}
