using System;


namespace Shiny.Net.Http
{
    public class HttpTransferMetrics
    {
        public HttpTransferMetrics(decimal percent,
                                   long bytesPerSec,
                                   TimeSpan estComp,
                                   DateTime timestamp)
        {
            this.PercentComplete = percent;
            this.BytesPerSecond = bytesPerSec;
            this.EstimatedCompletionTime = estComp;
            this.Timestamp = timestamp;
        }


        //      DateTime? lastTime;
        //      protected internal virtual void RunCalculations()
        //      {
        //	if (this.FileSize <= 0 || this.BytesTransferred <= 0)
        //		return;

        //	decimal raw = (this.BytesTransferred / this.FileSize) * 100;
        //	this.PercentComplete = Math.Round(raw, 2);

        //          //var elapsedTime = DateTime.Now - this.StartTime;
        //          if (this.lastTime != null)
        //          {
        //              var elapsedTime = DateTime.Now - this.lastTime.Value;
        //              this.BytesPerSecond = Convert.ToInt64(this.BytesTransferred / elapsedTime.TotalSeconds);

        //              var rawEta = this.FileSize / this.BytesPerSecond;
        //              this.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
        //          }
        //          this.lastTime = DateTime.Now;
        //}
        public decimal PercentComplete { get; }
        public long BytesPerSecond { get; }
        public TimeSpan EstimatedCompletionTime { get; }
        public DateTime Timestamp { get; }
    }
}
