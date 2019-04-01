using System;


namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransfer : NotifyPropertyChanged, IHttpTransfer
    {
        protected AbstractHttpTransfer(HttpTransferRequest request, bool upload)
        {
            this.Request = request;
            this.IsUpload = upload;
        }


        protected virtual void RunCalculations()
        {
			if (this.FileSize <= 0 || this.BytesTransferred <= 0)
				return;

			decimal raw = (this.BytesTransferred / this.FileSize) * 100;
			this.PercentComplete = Math.Round(raw, 2);

            var elapsedTime = DateTime.Now - this.StartTime;
			this.BytesPerSecond = this.BytesTransferred / elapsedTime.TotalSeconds;

			var rawEta = this.FileSize / this.BytesPerSecond;
			this.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
		}


        public HttpTransferRequest Request { get; }
        public bool IsUpload { get; }


        string id;
        public string Identifier
        {
            get => this.id;
            protected set => this.Set(ref this.id, value);
        }


        HttpTransferState status;
        public HttpTransferState Status
        {
            get => this.status;
            protected set => this.Set(ref this.status, value);
        }


        string remoteFile;
        public string RemoteFileName
        {
            get => this.remoteFile;
            protected set => this.Set(ref this.remoteFile, value);
        }


        long resumeOffset;
        public long ResumeOffset
        {
            get => this.resumeOffset;
            protected set => this.Set(ref this.resumeOffset, value);
        }


        long fileSize;
        public long FileSize
        {
            get => this.fileSize;
            protected set => this.Set(ref this.fileSize, value);
        }


        long bytesXfer;
        public long BytesTransferred
        {
            get => this.bytesXfer;
            protected set => this.Set(ref this.bytesXfer, value);
        }


        decimal percentComplete;
        public decimal PercentComplete
        {
            get => this.percentComplete;
            protected set => this.Set(ref this.percentComplete, value);
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


        double bytesPerSecond;
        public double BytesPerSecond
        {
            get => this.bytesPerSecond;
            protected set => this.Set(ref this.bytesPerSecond, value);
        }


        TimeSpan ctime;
        public TimeSpan EstimatedCompletionTime
        {
            get => this.ctime;
            protected set => this.Set(ref this.ctime, value);
        }


        public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;
    }
}
