using System;
using System.Threading;
using System.Threading.Tasks;
using BackgroundTasks;
using UIKit;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Jobs
{
    public class BgTasksJobManager : AbstractJobManager, IShinyStartupTask
    {
        const string EX_MSG = "Could not register background processing job. Shiny uses background processing when enabled in your info.plist.  Please follow the Shiny readme for Shiny.Core to properly register BGTaskSchedulerPermittedIdentifiers";
        bool registeredSuccessfully = false;


        public BgTasksJobManager(IServiceProvider container, IRepository repository) : base(container, repository)
        {
        }


        public void Start()
        {
            try
            {
                this.Register(this.GetIdentifier(false, false));
                this.Register(this.GetIdentifier(true, false));
                this.Register(this.GetIdentifier(false, true));
                this.Register(this.GetIdentifier(true, true));
                this.registeredSuccessfully = true;
            }
            catch (Exception ex)
            {
                Log.Write(new Exception(EX_MSG, ex));
            }
        }


        protected void Assert()
        {
            if (!this.registeredSuccessfully)
                throw new Exception(EX_MSG);
        }


        public static bool IsAvailable
        {
            get
            {
                var result = (
                    UIDevice.CurrentDevice.CheckSystemVersion(13, 0) &&
                    !iOSExtensions.IsSimulator &&
                    PlatformExtensions.HasBackgroundMode("processing")
                );
                return result;
            }
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
            this.Assert();

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