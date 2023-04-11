using System;
using System.IO;

namespace Shiny.Net.Http;


public static class HttpTransferExtensions
{
    public static double? PercentComplete(this HttpTransfer transfer)
    {
        if (transfer.BytesToTransfer == null)
            return null;

        var result = Math.Round((double)transfer.BytesTransferred / transfer.BytesToTransfer.Value, 2);
        return result;
    }


    public static void AssertValid(this HttpTransferRequest request)
    {
        if (request.Identifier.IsEmpty())
            throw new InvalidOperationException("Identifier is not set");

        if (request.IsUpload)
        {
            if (!File.Exists(request.LocalFilePath))
                throw new ArgumentException($"{request.LocalFilePath} does not exist");
        }
    }
}