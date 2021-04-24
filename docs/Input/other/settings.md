Title: Settings
---

Settings is installed into the Shiny container automatically.  Settings are a fairly essential service of any framework.  You can resolve this using dependency injection or using the Shiny.ShinyHost.Resolve.  The interface to use is Shiny.Settings.ISettings

## Interface: Shiny.Settings.ISettings

## To use, simply call:

```csharp
var int1 = settings.Get<int>("Key");
var int2 = settings.Get<int?>("Key");

settings.Set("Key", AnyObject); // your object is serialized under the hood
var obj = settings.Get<AnyObject>("Key");
```

## Strongly Typed Binding (works with all platforms - no fancy reflection that breaks on iOS)

```csharp
var myInpcObj = settings.Bind<MyInpcObject>(); // Your object must implement INotifyPropertyChanged
myInpcObj.SomeProperty = "Hi"; // everything is automatically synchronized to settings right here

//From your viewmodel
settings.Bind(this);

// make sure to unbind when your model is done
settings.UnBind(obj);
```