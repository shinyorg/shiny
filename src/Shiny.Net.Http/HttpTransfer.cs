using System;

namespace Shiny.Net.Http;


public record HttpTransfer(
    string Identifier,
    string Uri,
    string LocalFilePath,
    bool IsUpload,
    bool UseMeteredConnection,
    Exception? Exception,
    long FileSize,
    long BytesTransferred,
    HttpTransferState Status
)
{
    public double PercentComplete
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
