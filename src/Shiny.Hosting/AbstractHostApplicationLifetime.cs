using System;
using System.Threading;
using Microsoft.Extensions.Hosting;


namespace Shiny.Hosting
{
    public abstract class AbstractHostApplicationLifetime : IHostApplicationLifetime
    {
        protected AbstractHostApplicationLifetime()
        {
            this.AppStartedSource = new CancellationTokenSource();
            this.AppStoppedSource = new CancellationTokenSource();
            this.AppStoppingSource = new CancellationTokenSource();
        }


        protected CancellationTokenSource AppStartedSource { get; }
        protected CancellationTokenSource AppStoppingSource { get; }
        protected CancellationTokenSource AppStoppedSource { get; }

        public CancellationToken ApplicationStarted => this.AppStartedSource.Token;
        public CancellationToken ApplicationStopping => this.AppStoppingSource.Token;
        public CancellationToken ApplicationStopped => this.AppStoppedSource.Token;


        public virtual void StopApplication()
        {
            this.AppStoppingSource.Cancel();
            this.AppStoppedSource.Cancel();
        }
    }
}
