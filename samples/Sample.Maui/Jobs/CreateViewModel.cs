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

    string access;
    public string AccessStatus
    {
        get => this.access;
        private set => this.Set(ref this.access, value);
    }


    string jobName = "TestJob";
    public string JobName
    {
        get => this.jobName;
        set => this.Set(ref this.jobName, value);
    }


    int seconds = 10;
    public int SecondsToRun
    {
        get => this.seconds;
        set => this.Set(ref this.seconds, value);
    }


    string inetaccess = InternetAccess.None.ToString();
    public string RequiredInternetAccess
    {
        get => this.inetaccess;
        set => this.Set(ref this.inetaccess, value);
    }


    bool battery;
    public bool BatteryNotLow
    {
        get => this.battery;
        set => this.Set(ref this.battery, value);
    }


    bool charging;
    public bool DeviceCharging
    {
        get => this.charging;
        set => this.Set(ref this.charging, value);
    }


    bool repeat = true;
    public bool Repeat
    {
        get => this.repeat;
        set => this.Set(ref this.repeat, value);
    }


    bool foreground;
    public bool RunOnForeground
    {
        get => this.foreground;
        set => this.Set(ref this.foreground, value);
    }


    public override async void OnAppearing()
    {
        base.OnAppearing();
        this.AccessStatus = (await this.jobManager.RequestAccess()).ToString();
    }
}
