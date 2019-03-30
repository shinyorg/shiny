using System;
using Foundation;


namespace Shiny.Net.Http
{
    public class HttpTask : AbstractHttpTransfer
    {
        readonly NSUrlSessionDownloadTask task;


        public HttpTask(HttpTransferRequest request, NSUrlSessionDownloadTask task) : base(request, false)
        {
            this.task = task;
        }


        public override void Cancel()
        {
            this.Status = HttpTransferState.Cancelled;
            this.task.Cancel();
        }


        public void SetDownloadComplete(string tempLocation)
        {
            //this.LocalFilePath = tempLocation;
            this.Status = HttpTransferState.Completed;
        }


        public void SetResumeOffset(long resumeOffset, long expectedTotalBytes)
        {
            this.ResumeOffset = resumeOffset;
            this.BytesTransferred = resumeOffset;
            this.Status = HttpTransferState.Resumed;

            this.RunCalculations();
        }


        public void SetData(long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
        {
            this.RemoteFileName = this.task.Response?.SuggestedFilename;
            this.Status = HttpTransferState.Running;
            this.BytesTransferred = totalBytesWritten;
            this.FileSize = totalBytesExpectedToWrite;

            this.RunCalculations();
        }


        public void SetStatus(HttpTransferState status) => this.Status = status;
        public void SetError(NSError error) => this.Exception = new Exception(error.LocalizedDescription);
    }
}