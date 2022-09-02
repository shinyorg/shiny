namespace Sample;


public partial class SetupPage : SampleContentPage
{
    public SetupPage(SetupViewModel vm)
    {
        this.InitializeComponent();
        this.BindingContext = vm;
    }
}