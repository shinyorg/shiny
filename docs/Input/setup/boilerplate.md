Title: Boilerplate
Order: 1
Xref: boilerplate
---

Don't like the platform boilerplate?  This is easy to avoid with Shiny as of version 2.

First install the following nuget into your head projects!

<?# NugetShield "Shiny" /?>


Second, add the following assembly attribute in your head projects:

```csharp
[assembly: Shiny.ShinyApplication(
    ShinyStartupTypename = ""
    XamarinAppName = "YourNamespace.YourXamarinFormsApp"
    ExcludeJobs = false,
    ExcludeStartupTasks = false,
    ExcludeModules = false,
    ExcludeThirdParty = false,
    ExcludeServices = false
)]
```


This will auto-generate several things for you

1. All of the necessary Shiny hooks
2. Call Init on many 3rd party 
3. Auto-generate a Shiny startup file (if not given a custom startup class name)
    a. all jobs will be auto-registered
    b. any custom services will be auto-registered
    c. all delegates will be registered with their corresponding library (ie. services.UseGps<MyGpsDelegate>() )
4. Xamarin Forms app can be hooked and Init called for you automatically
5. Any classes marked with [Shiny.ShinyServiceAttribute] will be added and available to the container under all interfaces it implements


## Shiny also auto-adds popular 3rd party initialization for 

* [Xamarin Forms](https://github.com/xamarin/xamarin.forms)
* [Xamarin Essentials](https://github.com/xamarin/essentials)
* [ACR User Dialogs](https://github.com/aritchie/userdialogs)
* [AIForms Settings View](https://github.com/muak/AiForms.SettingsView)
* [XF Material](https://github.com/Baseflow/XF-Material-Library)
* [RG Popups](https://github.com/rotorgames/Rg.Plugins.Popup)
* [Microsoft Identity Client (MSAL)](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet)

## Special Considerations
1. All of your Android activities must be marked as a "partial class"
2. If you have an existing Android application class, you will have to add the Shiny hook yourself.  Please look at the [Getting Started with Android](android)
3. Some Shiny plugins require extra configuration that cannot be generated for the Shiny startup file such as "Shiny.Push".  Build errors will be given when this happens.  In this case, please follow the instructions for that particular library
