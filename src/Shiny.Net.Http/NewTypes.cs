using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace Shiny.Net.Http;


public record HttpTransferRequest(
    string Uri,
    bool IsUpload,
    FileInfo LocalFile,
    bool UseMeteredConnection = true,
    string? PostData = null,
    HttpMethod? HttpMethod = null,
    IDictionary<string, string>? Headers = null
);


public interface IHttpTransfer
{
    HttpTransferRequest Request { get; }

    void Pause();  // not for upload on ios
    void Resume(); // not for upload on ios
    void Cancel();

    HttpTransferState Status { get; }
    string Identifier { get; }
    ulong BytesTransferred { get; }

    // move to metrics, watch bytestransferred messages over x time against total bytes of request
    //ulong BytesPerSeconds { get; }
    //TimeSpan EstimateTimeRemaining { get; }

    public double PercentComplete
    {
        get
        {
            //if (this.BytesTransferred <= 0 || this.FileSize <= 0)
            //    return 0;

            //var raw = ((double)this.BytesTransferred / (double)this.FileSize);
            //return Math.Round(raw, 2);
            return 0;
        }
    }
}