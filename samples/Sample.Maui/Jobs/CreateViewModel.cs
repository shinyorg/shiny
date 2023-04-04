using Shiny.Jobs;
using Shiny.Notifications;

namespace Sample.Jobs;


public class CreateViewModel : ViewModel
{
    readonly IJobManager jobManager;
    readonly INotificationManager notifications;


    public CreateViewModel(
        BaseServices services,
        IJobManager jobManager,
        INotificationManager notificationManager
    ) : base(services)
    {
        this.jobManager = jobManager;
        this.notifications = notificationManager;

        this.CreateJob = ReactiveCommand.CreateFromTask(
            async _ =>
            {
                if (this.JobName.IsEmpty())
                {
                    await this.Dialogs.DisplayAlertAsync("ERROR", "Enter a job name", "OK");
                    return;
                }

                var access = await jobManager.RequestAccess();
                if (access != AccessState.Available)
                {
                    await this.Dialogs.DisplayAlertAsync("Error", "Job permissions failed: " + access, "OK");
                    return;
                }

                access = await this.notifications.RequestAccess();
                if (access != AccessState.Available)
                    await this.Dialogs.DisplayAlertAsync("Warning", "Invalid permissions for notifications, the job will not display them", "OK");

                var job = new JobInfo(
                    this.JobName.Trim(),
                    typeof(SampleJob), 
                    BatteryNotLow: this.BatteryNotLow,
                    DeviceCharging: this.DeviceCharging,
                    RunOnForeground: this.RunOnForeground,
                    RequiredInternetAccess: this.IsInternetRequired ? InternetAccess.Any : InternetAccess.None
                );
                this.jobManager.Register(job);
                await this.Navigation.GoBack();
            }
        );
    }


    public ICommand CreateJob { get; }
    public ICommand RunAsTask { get; }

    [Reactive] public string AccessStatus { get; private set; }
    [Reactive] public string JobName { get; set; } = "TestJob";
    [Reactive] public int SecondsToRun { get; set; } = 10;
    [Reactive] public bool IsInternetRequired { get; set; }
    [Reactive] public bool BatteryNotLow { get; set; }
    [Reactive] public bool DeviceCharging { get; set; }
    [Reactive] public bool RunOnForeground { get; set; } = true;


    public override async void OnAppearing()
    {
        base.OnAppearing();
        this.AccessStatus = (await this.jobManager.RequestAccess()).ToString();
    }
}
