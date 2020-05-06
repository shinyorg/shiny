Title: Getting Started
Order: 1
---

Localization is a great way of managing various text resources type (resx, json, xml, database, etc), from various origin (assembly, web service, database, etc), for various cultures, all mixed if needed!

[![NuGet](https://img.shields.io/nuget/v/Shiny.Localization.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Localization/)

## PLATFORMS

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

## Getting started
1. Install the NuGet package - [![NuGet](https://img.shields.io/nuget/v/Shiny.Localization.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Localization/)

2. In your [Shiny Startup](./startup) - add the following
```csharp

// This is an example where YourResourcesDesignerClass is the class name of the resx .Designer.cs auto generated file
public override void ConfigureServices(IServiceCollection services)
{
    services.UseLocalization<ResxTextProvider<YourResourcesDesignerClass>>();
}
```

3. Localize your application - e.g. the "binding way" 
3.1. Inject ILocalizationManager into your viewmodel (preferably into your base viewmodel)
```csharp

public class YourViewModel
{
    readonly ILocalizationManager localizationManager;
	
    public YouViewModel(ILocalizationManager localizationManager)
    {
		this.localizationManager = localizationManager;
    }

	public string this[string key] => this.localizationManager.GetText(key);
}
```

3.2. Just bind to [YourKeyToLocalize]
```xml

<ContentPage xmlns="http://xamarin.com/schemas/2014/forms" 
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:d="http://xamarin.com/schemas/2014/forms/design"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             mc:Ignorable="d"
             x:Class="Samples.Localization.LocalizationPage"
             Title="{Binding [LocalizationPage_Title]}">
	<ContentPage.Content>
    </ContentPage.Content>
</ContentPage>
```

3.3 You can change the language at runtime if needed by initializing the manager with the asked culture
```csharp

this.localizationManager.InitializeAsync(culture: null, tryParents: true, refreshAvailableCultures: false, token: default)
```

- culture: the culture you ask for localization(default: null = CurrentUICulture)
- tryParents: true to try with parent culture up to invariant when the asked one can't be found (default: true)
- refreshAvailableCultures: true to refresh AvailableCultures property during initialization (default: true)
- token: optional cancellation token

## Options builder
UseLocalization extension method comes with an options builder parameter to adjust some settings and behaviors
```csharp

// This is an example where YourResourcesDesignerClass is the class name of the resx .Designer.cs auto generated file
public override void ConfigureServices(IServiceCollection services)
{
    services.UseLocalization<ResxTextProvider<YourResourcesDesignerClass>>(optionsBuilder =>
                optionsBuilder.SomeOptionsHere(someParametersHere));
}
```

### Adjusting auto initialization settings
By default, auto initialization is on. 
It means that during app startup, it will load and cache all current UI culture localized key/values (or parent up to invariant culture if not available).
It will also refresh AvailableCultures property by checking all cultures with any localized key/values available.
You may want to change this behavior, here is how to do it:
```csharp

// This is an example where YourResourcesDesignerClass is the class name of the resx .Designer.cs auto generated file
public override void ConfigureServices(IServiceCollection services)
{
    services.UseLocalization<ResxTextProvider<YourResourcesDesignerClass>>(optionsBuilder =>
                optionsBuilder.WithAutoInitialization(autoInitialize: true, tryParents: true, refreshAvailableCultures: true, initializationCulture: null));
}
```
- autoInitialize: true to initialize localization on app startup from a background job or false for on demand initialization (default: true)
- tryParents: true to try with parent culture up to invariant when the asked one can't be found (default: true)
- refreshAvailableCultures: true to refresh AvailableCultures property during initialization (default: true)
- initializationCulture: force the CultureInfo you want to use by default during auto initialization

When auto initialization is turned off, you need to initialize the manager manualy when needed.

### Setting the default culture used as invariant
Even InvariantCulture resources (e.g. the main resx) are issued from a specific culture (but used for all missing).
You can tell the plugin wich culture your main text provider's invariant culture resources are issued from.
With that set, no more InvariantCulture item into AvailableCultures but YourDefaultCulture (useful for user understanding when binded to a Picker)
```csharp

// This is an example where YourResourcesDesignerClass is the class name of the resx .Designer.cs auto generated file
public override void ConfigureServices(IServiceCollection services)
{
    services.UseLocalization<ResxTextProvider<YourResourcesDesignerClass>>(optionsBuilder =>
                optionsBuilder.WithDefaultInvariantCulture(defaultInvariantCulture: YourCultureInfo));
}
```

### Adding some more text providers
Sometimes we need more than only one text provider.
In some scenarios, we could have a web and mobile shared resx in an assembly (e.g. common key/value for localized error message with key returned by server to mobile) plus a mobile specific resx (e.g. for views localization).
Another scenario could be when you want to provide default resx built-in for app startup and then pull down some fresh key/values into a database (database service must implement ITextProvider interface)
```csharp

// This is an example where YourResourcesDesignerClass is the class name of the resx .Designer.cs auto generated file and
// where YourDbTextProviderClass is your local data access service inheriting form ITextProvider interface
public override void ConfigureServices(IServiceCollection services)
{
    services.UseLocalization<ResxTextProvider<YourResourcesDesignerClass>>(optionsBuilder =>
                optionsBuilder.AddTextProvider<YourDbTextProviderClass>(invariantCulture: null));
}
```
You can add any custom text provider of your choice as long as it implements ITextProvider interface.
Text provider addition order is quite important as the manager will look for the key in this order (first found, first returned).
Suppose you get this:

|ResxTextProvider<YourResourcesDesignerClass>|YourDbTextProviderClass|
|--------|-------|
|Invariant (en)|Invariant (en)|
|French (fr)|French - France (fr-FR)|
|French - France (fr-FR)|Spanish (es)|

AvailableCultures will contain: English, French, French (France), Spanish

Then you ask for key "TestKey1" in fr-FR culture.
If ResxTextProvider<YourResourcesDesignerClass> contains the key it wins.
If not, YourDbTextProviderClass wins if it contains the key.
If not, nobody wins and the key is returned as a value.

### Mixing all
You can mix it all:
```csharp

public override void ConfigureServices(IServiceCollection services)
{
	var defaultCulture = CultureInfo.CreateSpecificCulture("en");
    services.UseLocalization<YourDbSyncTextProviderClass>(optionsBuilder =>
                optionsBuilder.WithAutoInitialization(initializationCulture: defaultCulture)
					.WithDefaultInvariantCulture(defaultCulture)
					.AddTextProvider<ResxTextProvider<YourSharedResourcesDesignerClass>>()
					.AddTextProvider<ResxTextProvider<YourMobileResourcesDesignerClass>>());
}
```

Here I'm saying:
- YourDbSyncTextProviderClass is my main text provider (kind of priority 1)
- Auto initialize with "en" default culture
- Set "en" culture as invariant culture
- Add mobile/server shared resx text provider (localized error message handling)
- Add mobile specific resx text provider (default app localization when the db one has no matching key - e.g. empty at first launch)