Title: Key/Value Stores
---

Shiny has several ways of storing key/values such as
* OS Settings/Preferences
* Secure Storage (NOTE: not currently support on UWP)
* In Memory (per session)
* File

## Key/Value

Shiny automatically registers the following stores
To access the various key/values, simply use Shiny.Stores.IKeyValueStoreFactory.



## Object Binding

But what makes key/value storage particularily unique in Shiny is the object binding.  You can bind
any object that support INotifyPropertyChanged & has public get/set values

```csharp
public class MySettings : Shiny.NotifyPropertyChanged 
{
    string myProperty?;
    public string? MyProperty 
    {
        get => this.myProperty;
        set => this.Set(ref this.myProperty, value);
    }
}
```

Now, to start using this is simple - first add this to your shiny startup

```csharp
services.AddSingleton<MySettings>();
```

Now to use it

```chsarp
// inject or 
var mySettings = ShinyHost.Resolve<MySettings>();

mySettings.MyProperty = null; // to remove it 

mySettings.MyProperty = "hello";
```

And to make things EVEN COOLER.  Object bindings like this also support different key/value stores by
adding the attribute [Shiny.ObjectBinder("alias of store")] on top of your class like so:

```csharp
[Shiny.ObjectBinderAttribute("secure")]
public class MySettings ...
```

There are 4 values currently supported by Shiny object binder - settings (default), secure, memory, and file


## Manual Binding

There may be cases where you want to manual restore an object, maybe you want to save viewmodel state in something like 

**EXAMPLE**
```csharp
// within your viewmodel
var binder = ShinyHost.Resolve<Shiny.Stores.IObjectBinder>();

// when starting or navigating to your viewmodel
binder.Bind(this);

// NOTE: most viewmodels are transient (as they should be), so be sure to unbind the instance upon your viewmodel being navigated away from
binder.UnBind(obj);
```