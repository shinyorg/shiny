using System;
using Shiny.Net.Http;


namespace Shiny.Testing.Net.Http
{
    public class HttpTransfer : IHttpTransfer
    {
        public HttpTransferRequest Request => throw new NotImplementedException();

        public string Identifier => throw new NotImplementedException();

        public HttpTransferState Status => throw new NotImplementedException();

        public Exception Exception => throw new NotImplementedException();

        public string RemoteFileName => throw new NotImplementedException();

        public long FileSize => throw new NotImplementedException();

        public long BytesTransferred => throw new NotImplementedException();

        public DateTime LastModified => throw new NotImplementedException();

        public double PercentComplete => throw new NotImplementedException();
    }
}
