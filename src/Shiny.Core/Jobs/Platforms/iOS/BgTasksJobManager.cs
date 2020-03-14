using System;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using UIKit;
using Shiny.Infrastructure;


namespace Shiny.Jobs
{
    public class BgTasksJobManager : AbstractJobManager
    {
        public BgTasksJobManager(IServiceProvider container, IRepository repository) : base(container, repository)
        {
            this.Register(this.GetIdentifier(false, false));
            this.Register(this.GetIdentifier(true, false));
            this.Register(this.GetIdentifier(false, true));
            this.Register(this.GetIdentifier(true, true));
        }


        public override Task<AccessState> RequestAccess()
        {
            var result = AccessState.Available;
            if (!UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                result = AccessState.NotSupported;

            else if (iOSExtensions.IsSimulator)
                result = AccessState.NotSupported;

            else if (!PlatformExtensions.HasBackgroundMode("processing"))
                result = AccessState.NotSetup;

            return Task.FromResult(result);
        }


        protected override void CancelNative(JobInfo jobInfo)
            => BGTaskScheduler.Shared.Cancel(jobInfo.Identifier);


        protected override void ScheduleNative(JobInfo jobInfo)
        {
            var identifier = this.GetIdentifier(
                jobInfo.DeviceCharging,
                jobInfo.RequiredInternetAccess == InternetAccess.Any
            );
            var request = new BGProcessingTaskRequest(identifier);
            request.RequiresExternalPower = jobInfo.DeviceCharging;
            request.RequiresNetworkConnectivity = jobInfo.RequiredInternetAccess == InternetAccess.Any;

            if (!BGTaskScheduler.Shared.Submit(request, out var e))
                throw new ArgumentException(e.LocalizedDescription.ToString());
        }


        protected void Register(string identifier)
        {
            BGTaskScheduler.Shared.Register(
                identifier,
                null,
                async task =>
                {
                    var cancelSrc = new CancellationTokenSource();
                    task.ExpirationHandler = cancelSrc.Cancel;

                    var result = await this.Run(task.Identifier, cancelSrc.Token);
                    task.SetTaskCompleted(result.Exception != null);
                }
            );
        }


        protected string GetIdentifier(bool extPower, bool network)
        {
            //"com.shiny.job"
            //"com.shiny.jobpower"
            //"com.shiny.jobnet"
            //"com.shiny.jobpowernet"
            var id = "com.shiny.job";
            if (extPower)
                id += "power";

            if (network)
                id += "net";

            return id;
        }
    }
}