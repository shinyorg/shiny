namespace Sample;


public class SetupViewModel : ViewModel
{
    readonly IPushManager pushManager;


    public SetupViewModel(IPushManager pushManager)
    {
        this.pushManager = pushManager;
        //this.apiClient = ShinyHost.Resolve<SampleApi>();

        this.RequestAccess = this.LoadingCommand(async () =>
        {
            var result = await this.pushManager.RequestAccess();
            this.AccessStatus = result.Status;
            this.Refresh();
//#if NATIVE
//            if (this.AccessStatus == AccessState.Available)
//                await this.Try(() => this.apiClient.Register(result.RegistrationToken!));
//#endif
        });

        this.UnRegister = this.LoadingCommand(async () =>
        {
            await this.pushManager.UnRegister();
            this.AccessStatus = AccessState.Disabled;
            this.Refresh();
//#if NATIVE
//            await this.Try(() => this.apiClient.UnRegister(deviceToken!));
//#endif
        });

        this.ResetBaseUri = new Command(() =>
        {
            //this.apiClient.Reset();
            //this.RaisePropertyChanged(nameof(this.BaseUri));
        });
    }


    public ICommand RequestAccess { get; }
    public ICommand UnRegister { get; }
    public ICommand ResetBaseUri { get; }
    public bool IsTagsSupported => this.pushManager.Tags != null;
    public string Implementation => this.pushManager.GetType().FullName;


    string regToken;
    public string RegToken
    {
        get => this.regToken;
        private set => this.Set(ref this.regToken, value);
    }


    AccessState access = AccessState.Unknown;
    public AccessState AccessStatus
    {
        get => this.access;
        private set => this.Set(ref this.access, value);
    }


    //public string BaseUri { get; set; }

    //public string BaseUri
    //{
    //    get => this.apiClient.BaseUri;
    //    set => this.apiClient.BaseUri = value;
    //}

#if NATIVE
    public bool IsNative => true;
#else
    public bool IsNative => false;
#endif


    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();
        this.Refresh();
    }


    void Refresh()
    {
        this.RegToken = this.pushManager.RegistrationToken ?? "-";
    }


    async Task Try(Func<Task> taskFunc)
    {
        try
        {
            await taskFunc();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
