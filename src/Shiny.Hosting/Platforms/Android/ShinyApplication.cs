using System;
using System.Threading;
using Android.App;
using Android.Runtime;
using Microsoft.Extensions.Hosting;


namespace Shiny.Hosting
{
    public class ShinyApplication : Application, IHostApplicationLifetime
    {
        protected ShinyApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
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


        public override void OnCreate()
        {
            base.OnCreate();
            this.AppStartedSource.Cancel();
        }


        public override void OnTerminate()
        {
            base.OnTerminate();
            this.StopApplication();
        }


        public void StopApplication()
        {
            this.AppStoppingSource.Cancel();
            this.AppStoppedSource.Cancel();
        }
    }
}
