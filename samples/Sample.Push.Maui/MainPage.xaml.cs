using Shiny.Hosting;

namespace Sample;


public partial class MainPage : TabbedPage
{
    public MainPage(
        MainViewModel vm,
        SetupPage setupPage,
        LogsPage logsPage,
        TagsPage tagsPage
    )
    {
        this.InitializeComponent();
        this.BindingContext = vm;

        this.Children.Add(setupPage);
        this.Children.Add(logsPage);
        if (vm.IsTagsSupported)
            this.Children.Add(tagsPage);
    }
}