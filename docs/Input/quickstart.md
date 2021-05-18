Title: Quick Start
Order: 1
---

## Setup

### WARNING: This method does require a later build of MSBuild - 16.8+ which is not yet available in most CI systems.  You can
use a utility like boots to install the latest beta of Mono, Xamarin Android, & Xamarin iOS on macOS to build your app.

1. The first thing is to install <?! NugetShield "Shiny" /?> to your application head project (ie. MyProject.iOS and MyProject.Android).  This library contains the Shiny.Core and source generators designed to plugin
all of the boilerplate code you'll need.

2. Add the following anywhere in each head project

```csharp
// NOTE THE USE OF THE FULL TYPE NAME INCLUDING NAMESPACE
[assembly: Shiny.ShinyApplication(
    ShinyStartupTypeName = "YourNamespace.YourApp",
    XamarinFormsAppTypeName = "YourNamespace.YourXamarinFormsApp"
)]
```  

3. In your shared code project.  Create a Shiny startup file:

<?! Startup ?>
// this is where you'll load things like BLE, GPS, etc - those are covered in other sections
// things like the jobs, environment, power, are all installed automatically
<?!/ Startup ?>


4. For iOS AppDelegate is marked partial.  If you have any methods that Shiny uses already implemented, you'll receive
a build warning to manually hook the method.

5. For Android, Shiny will automatically create the necessary Android application class.  For your main activity (or multiple activities),
ensure each one is marked as partial.  Shiny will again, auto hook the necessary methods.

6. Install any of the service modules Shiny has to offer and register them with your startup.  Be sure to follow any special
OS setup instructions in each module.

---
The Shiny source generators also automatically wire the following 3rd party libraries for you:

* [Xamarin Forms](https://github.com/xamarin/xamarin.forms)
* [Xamarin Essentials](https://github.com/xamarin/essentials)
* [ACR User Dialogs](https://github.com/aritchie/userdialogs)
* [AIForms Settings View](https://github.com/muak/AiForms.SettingsView)
* [XF Material](https://github.com/Baseflow/XF-Material-Library)
* [RG Popups](https://github.com/rotorgames/Rg.Plugins.Popup)
* [Microsoft Identity Client (MSAL)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)

If you have a truly custom application and you have a great deal of code in your AppDelegate or Android classes, you can manually
hook Shiny into these methods.  Take a look at [iOS Setup](xref:ios) and [Android Setup](xref:android) for how to do this.