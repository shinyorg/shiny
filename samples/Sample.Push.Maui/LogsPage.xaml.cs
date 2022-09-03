namespace Sample;


public partial class LogsPage : SampleContentPage
{
    public LogsPage(LogsViewModel vm)
    {
        this.InitializeComponent();
        this.BindingContext = vm;
    }
}