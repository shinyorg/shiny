using System;


namespace Shiny.Jobs
{
    public struct JobRunResult
    {
        public JobRunResult(bool hasNewData, JobInfo jobInfo, Exception? exception)
        {
            this.HasNewData = hasNewData;
            this.Job = jobInfo;
            this.Exception = exception;
        }


        public bool Success => this.Exception == null;
        public bool HasNewData { get; }
        public JobInfo Job { get; }
        public Exception? Exception { get; }
    }
}
