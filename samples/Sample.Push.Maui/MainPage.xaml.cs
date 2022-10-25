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

        this.Children.Add(new NavigationPage(setupPage) { Title = "Push" });
        this.Children.Add(new NavigationPage(logsPage) { Title = "Logs" });
        if (vm.IsTagsSupported)
            this.Children.Add(new NavigationPage(tagsPage) { Title = "Tags" });
    }
}