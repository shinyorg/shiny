using System;
using System.IO;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Hosting;

namespace Shiny.Net.Http;


public static class HttpTransferExtensions
{
    /// <summary>
    /// Tries to calculate HTTP transfer percentage complete - returns null if insufficient information
    /// </summary>
    /// <param name="transfer"></param>
    /// <returns></returns>
    public static double? PercentComplete(this HttpTransfer transfer)
    {
        if (transfer.BytesToTransfer == null)
            return null;

        var result = Math.Round((double)transfer.BytesTransferred / transfer.BytesToTransfer.Value, 2);
        return result;
    }


    /// <summary>
    /// Asserts that an HttpTransferRequest is valid
    /// </summary>
    /// <param name="request"></param>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
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