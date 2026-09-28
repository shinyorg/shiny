using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Net.Http.Infrastructure;

namespace Shiny.Net.Http;


// tus on a background NSURLSession: the session only runs single upload/download tasks, so every PATCH is its
// own background upload task sent from a temp file holding that slice of the upload. The small POST (create)
// and HEAD (offset) requests go over HttpClient, and each finished chunk starts the next one from
// DidCompleteWithError.
public partial class HttpTransferManager
{
    static readonly TimeSpan TusNetworkRetryDelay = TimeSpan.FromSeconds(30);

    HttpClient? tusHttpClient;
    HttpClient TusHttpClient => this.tusHttpClient ??= new HttpClient();


    /// <summary>
    /// Brings a tus upload up to date with the server - creating it if needed, otherwise asking for its
    /// offset - and starts the next chunk. Failures are reported through the delegate, never thrown.
    /// </summary>
    async Task ContinueTus(string identifier)
    {
        var ht = repository.Get<HttpTransfer>(identifier);
        if (ht == null || ht.Status == HttpTransferState.Paused)
            return;

        try
        {
            var request = ht.Request;
            var uploadLength = new FileInfo(request.LocalFilePath).Length;
            var uploadUri = ht.TusUploadUri;
            long offset = 0;

            if (uploadUri != null)
            {
                var serverOffset = await TusProtocol
                    .GetOffset(this.TusHttpClient, request, uploadUri, default)
                    .ConfigureAwait(false);

                if (serverOffset == null)
                {
                    logger.StandardInfo(identifier, "tus upload no longer exists on the server - creating it again");
                    uploadUri = null;
                }
                else
                {
                    offset = serverOffset.Value;
                }
            }

            if (uploadUri == null)
            {
                uploadUri = await TusProtocol
                    .Create(this.TusHttpClient, request, uploadLength, default)
                    .ConfigureAwait(false);

                logger.StandardInfo(identifier, $"Created tus upload {uploadUri}");
                var created = repository.Get<HttpTransfer>(identifier);
                if (created == null)
                    return; // cancelled while the upload was being created

                repository.Set(created with { TusUploadUri = uploadUri });
            }

            this.StartTusChunk(identifier, offset);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            this.OnTusNetworkError(identifier, ex);
        }
        catch (HttpRequestException ex)
        {
            this.OnError(ht, (int)ex.StatusCode!.Value, ex);
        }
        catch (Exception ex)
        {
            this.OnError(ht, 0, ex);
        }
    }


    /// <summary>
    /// Sends the chunk that starts at <paramref name="offset"/> as a background upload task, or finishes the
    /// transfer when the server already has every byte.
    /// </summary>
    void StartTusChunk(string identifier, long offset)
    {
        // re-read - the user may have paused or cancelled while a request was in flight
        var ht = repository.Get<HttpTransfer>(identifier);
        if (ht == null || ht.Status == HttpTransferState.Paused || ht.TusUploadUri == null)
            return;

        var request = ht.Request;
        var uploadLength = new FileInfo(request.LocalFilePath).Length;
        if (offset >= uploadLength)
        {
            logger.LogInformation($"Transfer {identifier} was completed");
            this.OnFinish(ht);
            return;
        }

        var count = TusProtocol.GetChunkLength(request, offset, uploadLength);
        var tempPath = platform.GetUploadTempFilePath(request);
        WriteSlice(request.LocalFilePath, tempPath, offset, count);

        var native = request.ToNative(
            ht.TusUploadUri,
            "PATCH",
            new Dictionary<string, string>
            {
                [TusProtocol.TusResumableHeader] = TusProtocol.Version,
                [TusProtocol.UploadOffsetHeader] = offset.ToString(CultureInfo.InvariantCulture),
                ["Content-Type"] = TusProtocol.OffsetContentType
            }
        );
        configurator?.Configure(native, request);

        var task = this.Session.CreateUploadTask(native, NSUrl.CreateFileUrl(tempPath, null));
        task.TaskDescription = identifier;
        task.Resume();

        repository.Set(ht with
        {
            Status = HttpTransferState.InProgress,
            BytesToTransfer = uploadLength,
            BytesTransferred = offset
        });
    }


    /// <summary>
    /// Handles a finished PATCH task. Returns true when the task was a tus chunk (handled here).
    /// </summary>
    bool TryCompleteTusChunk(HttpTransfer ht, NSUrlSessionTask task)
    {
        if (ht.Request.Type != TransferType.UploadTus)
            return false;

        var statusCode = task.GetStatusCode();
        if (task.Error != null)
        {
            if (task.IsCancelled())
            {
                this.OnCancel(ht);
            }
            else
            {
                var e = task.Error;
                this.OnTusNetworkError(ht.Identifier, new InvalidOperationException($"HTTP Transfer Error - {e.LocalizedDescription} - {e.LocalizedFailureReason}"));
            }
        }
        else if (statusCode == 409)
        {
            // the server's offset differs from the one we sent - ask it and carry on from there
            _ = this.ContinueTus(ht.Identifier);
        }
        else if (statusCode < 200 || statusCode > 299)
        {
            this.OnError(ht, statusCode, new InvalidOperationException("HTTP Transfer Error - Invalid Status Code: " + statusCode));
        }
        else if (Int64.TryParse(task.GetResponseHeader(TusProtocol.UploadOffsetHeader), NumberStyles.None, CultureInfo.InvariantCulture, out var offset))
        {
            try
            {
                this.StartTusChunk(ht.Identifier, offset);
            }
            catch (Exception ex)
            {
                this.OnError(ht, 0, ex);
            }
        }
        else
        {
            // no Upload-Offset in the response - ask for it rather than guess
            _ = this.ContinueTus(ht.Identifier);
        }
        return true;
    }


    /// <summary>
    /// The upload's position within the whole file for a PATCH task - its <c>Upload-Offset</c> request header.
    /// </summary>
    static long GetTusChunkOffset(NSUrlSessionTask task)
        => Int64.TryParse(task.GetRequestHeader(TusProtocol.UploadOffsetHeader), NumberStyles.None, CultureInfo.InvariantCulture, out var offset)
            ? offset
            : 0;


    void OnTusNetworkError(string identifier, Exception ex)
    {
        logger.LogWarning(ex, "tus upload {Identifier} lost its connection - it will continue from the server's offset", identifier);

        var ht = repository.Get<HttpTransfer>(identifier);
        if (ht == null || ht.Status == HttpTransferState.Paused)
            return;

        ht = ht with { Status = HttpTransferState.PausedByNoNetwork };
        repository.Set(ht);
        this.UpdateReceived?.Invoke(this, new(
            ht.Request,
            ht.Status,
            new TransferProgress(0, ht.BytesToTransfer, ht.BytesTransferred),
            null
        ));

        // retried here while the app stays alive, and from Start() on the next launch
        _ = Task.Delay(TusNetworkRetryDelay).ContinueWith(_ =>
        {
            var current = repository.Get<HttpTransfer>(identifier);
            if (current?.Status == HttpTransferState.PausedByNoNetwork)
                _ = this.ContinueTus(identifier);
        });
    }


    static void WriteSlice(string sourcePath, string destPath, long offset, long count)
    {
        using var source = File.OpenRead(sourcePath);
        using var dest = File.Create(destPath);
        source.Position = offset;

        var buffer = new byte[81920];
        var remaining = count;
        while (remaining > 0)
        {
            var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read == 0)
                throw new IOException($"Upload file ended {remaining} bytes early - was it changed after the upload was queued?");

            dest.Write(buffer, 0, read);
            remaining -= read;
        }
    }
}
