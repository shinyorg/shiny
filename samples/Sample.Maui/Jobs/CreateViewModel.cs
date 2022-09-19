using Shiny;
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

        this.CreateJob = new Command(
            async _ =>
            {
                if (this.JobName.IsEmpty())
                {
                    await this.Alert("Enter a job name");
                    return;
                }
                if (this.SecondsToRun < 10)
                {
                    await this.Alert("Must be great than 10 seconds");
                    return;
                }

                await this.notifications.RequestAccess();

                var job = new JobInfo(typeof(SampleJob), this.JobName.Trim())
                {
                    Repeat = this.Repeat,
                    BatteryNotLow = this.BatteryNotLow,
                    DeviceCharging = this.DeviceCharging,
                    RunOnForeground = this.RunOnForeground,
                    RequiredInternetAccess = (InternetAccess)Enum.Parse(typeof(InternetAccess), this.RequiredInternetAccess)
                };
                job.SetParameter("SecondsToRun", this.SecondsToRun);
                await this.jobManager.Register(job);
                await this.Navigation.PopAsync();
            }
        );


        this.ChangeRequiredInternetAccess = new Command(async () =>
        {
            this.RequiredInternetAccess = await this.Choose(
                "Internet Access",
                InternetAccess.None.ToString(),
                InternetAccess.Any.ToString(),
                InternetAccess.Unmetered.ToString()
            );
        });
    }


    public ICommand CreateJob { get; }
    public ICommand RunAsTask { get; }
    public ICommand ChangeRequiredInternetAccess { get; }

    [Reactive] public string AccessStatus { get; private set; }
    [Reactive] public string JobName { get; set; } = "TestJob";
    [Reactive] public int SecondsToRun { get; set; } = 10;
    [Reactive] public string RequiredInternetAccess { get; set; } = InternetAccess.None.ToString();
    [Reactive] public bool BatteryNotLow { get; set; }
    [Reactive] public bool DeviceCharging { get; set; }
    [Reactive] public bool Repeat { get; set; } = true;
    [Reactive] public bool RunOnForeground { get; set; } = true;


    //public override async void OnAppearing()
    //{
    //    base.OnAppearing();
    //    this.AccessStatus = (await this.jobManager.RequestAccess()).ToString();
    //}
}
