using Uno.Extensions.Hosting;

namespace Sample.Uno.Presentation;


public sealed partial class Shell : UserControl, IContentControlProvider
{
    public Shell() => this.InitializeComponent();

    public ContentControl ContentControl => this.Splash;
}


public class ShellViewModel;
