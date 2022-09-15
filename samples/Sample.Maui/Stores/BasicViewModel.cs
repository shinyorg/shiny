namespace Sample.Stores;


public class BasicViewModel : ViewModel
{
    readonly IAppSettings appSettings;

    public BasicViewModel(BaseServices services, IAppSettings settings) : base(services)
    {
        this.appSettings = settings;
    }


    [Reactive] public bool IsChecked { get; set; }
    [Reactive] public string YourText { get; set; }
    [Reactive] public DateTime? LastUpdated { get; set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.appSettings
            .WhenAnyProperty()
            .Subscribe(_ =>
            {
                this.IsChecked = this.appSettings.IsChecked;
                this.YourText = this.appSettings.YourText;
                this.LastUpdated = this.appSettings.LastUpdated;
            })
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }
}
