using System;


namespace Shiny.IO
{
    public class FileProgress
    {
        public FileProgress(long bytesCompleted, long fileSize, DateTime startUtc)
        {
            this.FileSize = fileSize;
            this.BytesCompleted = bytesCompleted;
            this.PercentComplete = Convert.ToInt32(bytesCompleted * 100.0 / fileSize);
            this.TimeSpent = DateTime.UtcNow.Subtract(startUtc);
            this.BytesPerSecond = bytesCompleted / this.TimeSpent.TotalSeconds; // TODO: should be a sampling, not overall
            this.TimeRemaining = TimeSpan.FromSeconds(this.BytesCompleted / this.BytesPerSecond);
        }


        public TimeSpan TimeRemaining { get; }
        public TimeSpan TimeSpent { get; }
        public int PercentComplete { get; }
        public long FileSize { get; }
        public long BytesCompleted { get; }
        public double BytesPerSecond { get; }
    }
}
