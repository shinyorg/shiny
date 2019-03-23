# ACR Settings Plugin for Xamarin and Windows
A cross platform settings plugin for Xamarin and Windows.  Unlike other setting libraries in the wild, this library provides several unique features

### [SUPPORT THIS PROJECT](https://github.com/aritchie/home)


[Change Log - April 12, 2018](changelog.md)

[![NuGet](https://img.shields.io/nuget/v/Acr.Settings.svg?maxAge=2592000)](https://www.nuget.org/packages/Acr.Settings/)
[![Build status](https://dev.azure.com/allanritchie/Plugins/_apis/build/status/Settings)](https://dev.azure.com/allanritchie/Plugins/_build/latest?definitionId=11)


## Features
* You can store/retrieve just about any type of object (thanks to Newtonsoft.Json)
* You can monitor for changes using the Changed event
* iCloud Settings Provider
* You can use roaming profiles which is useful for:
    * iOS app groups
    * iOS extensions
    * iWatch
    * Android Wear

## To use, simply call:

    var int1 = CrossSettings.Current.Get<int>("Key");
    var int2 = CrossSettings.Current.Get<int?>("Key");

    CrossSettings.Current.Set("Key", AnyObject); // converts to JSON
    var obj = CrossSettings.Current.Get<AnyObject>("Key");

## Strongly Typed Binding (works with all platforms - no fancy reflection that breaks on iOS)

    var myInpcObj = CrossSettings.Current.Bind<MyInpcObject>(); // Your object must implement INotifyPropertyChanged
    myInpcObj.SomeProperty = "Hi"; // everything is automatically synchronized to settings right here

    //From your viewmodel
    CrossSettings.Current.Bind(this);

    // make sure to unbind when your model is done
    CrossSettings.Current.UnBind(obj);

## To supply your own implementation:

    CrossSettings.Current = new YourImplementationInheritingISettings();


## Monitor setting changes:

    CrossSettings.Current.Changed += (sender, args) => {
        Console.WriteLine(args.Action);
        Console.WriteLine(args.Key);
        Console.WriteLine(args.Value);
    };

### Dependency Injection:

*Autofac*

        containerBuilder.Register(x => CrossSettings.Current).As<ISettings>().SingleInstance();
        
        OR for strongly typed

        containerBuilder.Register(_ => CrossSettings.Current.Bind<YouAppSettings>()).AsSelf().SingleInstance();


