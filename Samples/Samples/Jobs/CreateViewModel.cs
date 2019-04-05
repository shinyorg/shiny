using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Prism.Navigation;
using Shiny;
using Shiny.Jobs;
using Samples.ShinySetup;


namespace Samples.Jobs
{
    public class CreateViewModel : ViewModel
    {
        readonly IJobManager jobManager;
        readonly IUserDialogs dialogs;


        public CreateViewModel(IJobManager jobManager,
                               INavigationService navigator,
                               IUserDialogs dialogs)
        {
            this.jobManager = jobManager;
            this.dialogs = dialogs;

            var valObs = this.WhenAny(
                x => x.JobName,
                x => x.JobLoopCount,
                (name, loops) =>
                    !name.GetValue().IsEmpty() &&
                    loops.GetValue() >= 10
            );

            this.CreateJob = ReactiveCommand.CreateFromTask(
                async _ =>
                {
                    var job = new JobInfo
                    {
                        Identifier = this.JobName.Trim(),
                        Type = typeof(SampleAllDelegate),
                        Repeat = this.Repeat,
                        BatteryNotLow = this.BatteryNotLow,
                        DeviceCharging = this.DeviceCharging,
                        RequiredInternetAccess = (InternetAccess)Enum.Parse(typeof(InternetAccess), this.RequiredInternetAccess)
                    };
                    job.SetValue("LoopCount", this.JobLoopCount);
                    await this.jobManager.Schedule(job);
                    await navigator.GoBack();
                },
                valObs
            );

            this.RunAsTask = ReactiveCommand.Create(
                () => this.jobManager.RunTask(this.JobName + "Task", async _ =>
                {
                    this.dialogs.Toast("Task Started");
                    for (var i = 0; i < this.JobLoopCount; i++)
                    {
                        await Task.Delay(1000).ConfigureAwait(false);
                    }
                    this.dialogs.Toast("Task Finished");
                }),
                valObs
            );

            this.ChangeRequiredInternetAccess = ReactiveCommand.Create(() =>
            {
                var cfg = new ActionSheetConfig()
                    .Add(
                        InternetAccess.None.ToString(),
                        () => this.RequiredInternetAccess = InternetAccess.None.ToString()
                    )
                    .Add(
                        InternetAccess.Any.ToString(),
                        () => this.RequiredInternetAccess = InternetAccess.Any.ToString()
                    )
                    .Add(
                        InternetAccess.Direct.ToString(),
                        () => this.RequiredInternetAccess = InternetAccess.Direct.ToString()
                    )
                    .SetCancel();
                this.dialogs.ActionSheet(cfg);
            });
        }


        public ICommand CreateJob { get; }
        public ICommand RunAsTask { get; }
        public ICommand ChangeRequiredInternetAccess { get; }
        [Reactive] public string AccessStatus { get; private set; }
        [Reactive] public string JobName { get; set; } = "TestJob";
        [Reactive] public int JobLoopCount { get; set; } = 10;
        [Reactive] public string RequiredInternetAccess { get; set; } = InternetAccess.None.ToString();
        [Reactive] public bool BatteryNotLow { get; set; }
        [Reactive] public bool DeviceCharging { get; set; }
        [Reactive] public bool Repeat { get; set; } = true;


        public override async void OnAppearing()
        {
            base.OnAppearing();
            var r = await this.jobManager.RequestAccess();
            this.AccessStatus = r.ToString();
        }
    }
}
