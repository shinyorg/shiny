using System;


namespace Shiny.Jobs
{
    public struct JobRunResult
    {
        public JobRunResult(JobInfo? jobInfo, Exception? exception)
        {
            this.Job = jobInfo;
            this.Exception = exception;
        }


        public bool Success => this.Exception == null;
        public JobInfo? Job { get; }
        public Exception? Exception { get; }
    }
}
