using System;
using System.IO;
using Foundation;
using Microsoft.Extensions.Logging;
using Security;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;

public partial class HttpTransferManager
{
    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    {
        var id = task.TaskDescription!;
        var ht = repository.Get<HttpTransfer>(id);

        if (ht == null)
        {
            logger.NoTransferFound(id);
        }
        else
        {
            logger.TransferProgress(id, totalBytesSent, totalBytesExpectedToSend);

            ht = ht with
            {
                BytesToTransfer = totalBytesExpectedToSend,
                BytesTransferred = totalBytesSent,
                Status = HttpTransferState.InProgress
            };

            try
            {
                repository.Update(ht);
                var bps = (int)(task.Progress?.Throughput ?? 0);
                this.transferSubj.OnNext(new(
                    ht.Request,
                    ht.Status,
                    new(bps, totalBytesExpectedToSend, totalBytesSent),
                    null
                ));
            }
            catch (RepositoryException) 
            {
                // ignore
            }
        }
    }


    public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    {
        var ht = repository.Get<HttpTransfer>(downloadTask.TaskDescription!);

        if (ht == null)
        {
            logger.NoTransferFound(downloadTask.TaskDescription!);
        }
        else
        {
            logger.TransferProgress(downloadTask.TaskDescription!, totalBytesWritten, totalBytesExpectedToWrite);

            ht = ht with
            {
                BytesToTransfer = totalBytesExpectedToWrite,
                BytesTransferred = totalBytesWritten,
                Status = HttpTransferState.InProgress
            };

            try
            {
                repository.Update(ht);
                var bps = (int)(downloadTask.Progress?.Throughput ?? 0);

                this.transferSubj.OnNext(new(
                    ht.Request,
                    ht.Status,
                    new(bps, totalBytesExpectedToWrite, totalBytesWritten),
                    null
                ));
            }
            catch (RepositoryException)
            {
                // ignore
            }
        }
    }
    

    public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        if (downloadTask.HasError())
            return;

        var id = downloadTask.TaskDescription!;
        var ht = repository.Get<HttpTransfer>(id);

        if (ht == null)
        {
            logger.NoTransferFound(id);
        }
        else
        {
            logger.StateMethod(id);
            File.Copy(location.Path!, ht.Request.LocalFilePath, true);
            this.OnFinish(ht);
        }
    }
    

    public override void DidReceiveChallenge(
        NSUrlSession session,
        NSUrlSessionTask task,
        NSUrlAuthenticationChallenge challenge,
        Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler
    )
    {
        logger.LogDebug("DidReceiveChallenge");
        
        // challenge.ProtectionSpace.AuthenticationMethod    
        
        completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null!);
    }

    
    public override void DidBecomeInvalid(NSUrlSession session, NSError error)
    {
        logger.LogDebug("DidBecomeInvalid");
        this.nsUrlSession = null;

        if (error != null)
        {
            logger.LogError(
                new InvalidOperationException(error.LocalizedDescription),
                "DidBecomeInvalid reported an error"
            );
        }
    }


    //// this is tough to implement due to NSInputStream & CFStream delegates
    //// reauthorize?
    ////public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
    ////{
    ////    var transfer = task.FromNative();
    ////    var file = new FileInfo(transfer.LocalFilePath);
    ////    //var stream = new BodyStream(file);
    ////    //completionHandler(stream);
    ////}


    //public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
    //{
    //    this.logger.LogDebug("DidResume");
    //    //this.onEvent.OnNext(downloadTask.FromNative());
    //}


    public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        logger.LogInformation("DidFinishEventsForBackgroundSession");
        this.completionHandler?.Invoke();
    }


    public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError? error)
    {
        var ht = repository.Get<HttpTransfer>(task.TaskDescription!);
        if (ht == null)
        {
            logger.NoTransferFound(task.TaskDescription!);
            return;
        }

        logger.LogDebug("DidCompleteWithError: " + task.State);
        switch (task.State)
        {
            case NSUrlSessionTaskState.Suspended:
                ht = ht with { Status = HttpTransferState.Paused };
                repository.Update(ht);

                this.transferSubj.OnNext(new(
                    ht.Request,
                    ht.Status,
                    TransferProgress.Empty,
                    null
                ));
                break;

            case NSUrlSessionTaskState.Canceling:
                this.OnCancel(ht);
                break;

            case NSUrlSessionTaskState.Completed:
            default:
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
                        var msg = $"HTTP Transfer Error - {e?.LocalizedDescription} - {e?.LocalizedFailureReason}";
                        this.OnError(ht, statusCode, new InvalidOperationException(msg));
                    }
                }
                else if (statusCode < 200 || statusCode > 299)
                {
                    this.OnError(ht, statusCode, new InvalidOperationException("HTTP Transfer Error - Invalid Status Code: " + statusCode));
                }
                else if (ht.Request.Type.IsUpload())
                {
                    // If download, let DidFinishDownloading call this
                    logger.LogInformation($"Transfer {ht.Identifier} was completed");
                    this.OnFinish(ht);
                }
                break;
        }
    }
}