using System;


namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransfer : NotifyPropertyChanged, IHttpTransfer
    {
        protected AbstractHttpTransfer(HttpTransferRequest request)
        {
            this.Request = request;
            if (request.IsUpload)
            {
                this.FileSize = request.LocalFile.Length;
                this.RemoteFileName = request.LocalFile.Name;
            }
        }


        DateTime? lastTime;
        protected internal virtual void RunCalculations()
        {
			if (this.FileSize <= 0 || this.BytesTransferred <= 0)
				return;

			decimal raw = (this.BytesTransferred / this.FileSize) * 100;
			this.PercentComplete = Math.Round(raw, 2);

            //var elapsedTime = DateTime.Now - this.StartTime;
            if (this.lastTime != null)
            {
                var elapsedTime = DateTime.Now - this.lastTime.Value;
                this.BytesPerSecond = Convert.ToInt64(this.BytesTransferred / elapsedTime.TotalSeconds);
            }
            this.lastTime = DateTime.Now;
			var rawEta = this.FileSize / this.BytesPerSecond;
			this.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
		}


        public HttpTransferRequest Request { get; }
        public bool IsUpload => this.Request.IsUpload;


        string id;
        public string Identifier
        {
            get => this.id;
            internal set => this.Set(ref this.id, value);
        }


        HttpTransferState status;
        public HttpTransferState Status
        {
            get => this.status;
            internal set => this.Set(ref this.status, value);
        }


        string remoteFile;
        public string RemoteFileName
        {
            get => this.remoteFile;
            internal set => this.Set(ref this.remoteFile, value);
        }


        long resumeOffset;
        public long ResumeOffset
        {
            get => this.resumeOffset;
            internal set => this.Set(ref this.resumeOffset, value);
        }


        long fileSize;
        public long FileSize
        {
            get => this.fileSize;
            internal set => this.Set(ref this.fileSize, value);
        }


        long bytesXfer;
        public long BytesTransferred
        {
            get => this.bytesXfer;
            internal set => this.Set(ref this.bytesXfer, value);
        }


        decimal percentComplete;
        public decimal PercentComplete
        {
            get => this.percentComplete;
            internal set => this.Set(ref this.percentComplete, value);
        }


        Exception exception;
        public Exception Exception
        {
            get => this.exception;
            set
            {
                this.exception = value;
                this.Status = HttpTransferState.Error;
                this.RaisePropertyChanged();
            }
        }


        long bytesPerSecond;
        public long BytesPerSecond
        {
            get => this.bytesPerSecond;
            internal set => this.Set(ref this.bytesPerSecond, value);
        }


        TimeSpan ctime;
        public TimeSpan EstimatedCompletionTime
        {
            get => this.ctime;
            internal set => this.Set(ref this.ctime, value);
        }
    }
}
