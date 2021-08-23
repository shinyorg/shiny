Title: Getting Started
Order: 1
---

<?! PackageInfo "Shiny.Framework" /?>

> Shiny Framework brings together Prism, Reactive UI, and Shiny with a bunch of best practices 


1. Install <?# NugetShield "Shiny.Framework" /?> 
2. Change your App.xaml to the following
```xml
<shiny:FrameworkApplication xmlns="http://xamarin.com/schemas/2014/forms"
                            xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                            xmlns:shiny="http://shinylib.net"
                            x:Class="YourNamespace.YourApp">
    <!--you can include any of your resources here if you want-->
</shiny:FrameworkApplication>
```
3. Change your App.xaml.cs to use the following:
```csharp
        
using Shiny;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace YourNamespace
{
    public partial class YourApp : FrameworkApplication
    {
        // you only need this if you have anything in your application resources
        protected override void Initialize()
        {
            this.InitializeComponent();
            base.Initialize();
        }
    }
}
```
4. Create a startup file and inherit the following:
```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Navigation;

namespace YourNamespace
{
    public class YourStartup : Shiny.FrameworkStartup
    {
        public override void ConfigureApp(IContainerRegistry containerRegistry)
        {
            // configure all of your Prism navigation and foreground only services here!
        }


        public override Task RunApp(INavigationService navigator)
        {
            // use the navigator to go to your starting page
        }


        protected override void Configure(ILoggingBuilder builder, IServiceCollection services)
        {
            // register all of your Shiny services and background infrastructure here
        }
    }
}
```
5. With the above startup file, initialize Shiny just like normal (NOTE: on iOS, make sure Shiny is initialized before any Xamarin Forms call!!!)