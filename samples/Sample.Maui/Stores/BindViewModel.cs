using Shiny.Stores;

namespace Sample.Stores;


public class BindViewModel : ViewModel
{
    readonly IObjectStoreBinder binder;


    public BindViewModel(BaseServices services, IObjectStoreBinder binder) : base(services)
    {
        this.binder = binder;
    }


    [Reactive] public bool IsChecked { get; set; }
    [Reactive] public string YourText { get; set; }
    [Reactive] public DateTime? LastUpdated { get; set; }


    public override Task InitializeAsync(INavigationParameters parameters)
    {
        this.binder.Bind(this, "settings");
        this.WhenAnyProperty()
            .Subscribe(_ => this.LastUpdated = DateTime.Now)
            .DisposedBy(this.DestroyWith);

        return base.InitializeAsync(parameters);
    }


    public override void Dispose()
    {
        this.binder.UnBind(this);
        base.Dispose();
    }
}
