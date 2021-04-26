Title: Quick Start
Order: 1
---

## Setup

1. The first thing is to install Shiny.Core as it used by all of the Shiny libraries (or Shiny which contains the code gen, but we'll get to that later).  

2. In your shared code project.  Create a Shiny startup file:

<?! Startup ?>
// this is where you'll load things like BLE, GPS, etc - those are covered in other sections
// things like the jobs, environment, power, are all installed automatically
<?!/ Startup ?>

As another alternative, you can have Shiny generate a startup file automatically at compile time.

### Android & iOS

1. The best option is to install the <?# NugetShield "Shiny" /?> in your head Android, iOS, and UWP head projects.  
2. In each head project, add the following anywhere in your project:

```csharp
// NOTE THE USE OF THE FULL TYPE NAME INCLUDING NAMESPACE
[assembly: Shiny.ShinyApplication(
    ShinyStartupTypeName = "YourNamespace.YourApp",
    XamarinFormsAppTypeName = "YourNamespace.YourXamarinFormsApp"
)]
```

3. iOS: Make sure your AppDelegate is marked as a partial class.  If you implement any of the methods in it, consider removing them.  If you have any custom code in them, take a look at [boilerplate](xref:boilerplate) for more custom scenarios
3. Android: Consider deleting any Android application classes you have and make sure all of your activities are marked partial.

Out of the box, Shiny automatically adds all of the services for Jobs, file system, power monitoring, and key/value storage (as well as several other services need by the Shiny internals)
