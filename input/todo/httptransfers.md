# ACR HTTP Transfers Plugin for Xamarin and Windows
_Cross platform HTTP download/upload manager_

### [SUPPORT THIS PROJECT](https://github.com/aritchie/home)

[Change Log](changelog.md)

[![NuGet](https://img.shields.io/nuget/v/Plugin.HttpTransferTasks.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.HttpTransferTasks/)
[![Build status](https://dev.azure.com/allanritchie/Plugins/_apis/build/status/HttpTransferTasks)](https://dev.azure.com/allanritchie/Plugins/_build/latest?definitionId=0)


## SUPPORTED PLATFORMS
|Platform|Version|
|--------|-------|
iOS|7+
Android|4.3+
Windows UWP|16299+
.NET Standard|2.0


## FEATURES

* Background Uploads & Downloads on each platform
* Supports transfer filtering based on metered connections (iOS & UWP only at the moment)
* Event Based Metrics
  * Percentage Complete
  * Total Bytes Expected
  * Total Bytes Transferred
  * Transfer Speed (Bytes Per Second)
  * Estimated Completion Time

## SETUP

Be sure to install the Plugin.HttpTransferTasks nuget package in all of your main platform projects as well as your core/PCL project

[![NuGet](https://img.shields.io/nuget/v/Plugin.HttpTransferTasks.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.HttpTransferTasks/)

**Android**

Add the following to your AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.WAKE_LOCK"/>
```

## HOW TO USE BASICS

```csharp

// discover some devices
var task = CrossHttpTransfers.Current.Upload("http://somewheretosend.com", "<YOUR LOCAL FILEPATH>");


// when performing downloads, it is necessary to listen to where the temp file lands (this is due to iOS)
var task = CrossHttpTransfers.Current.Download("http://somewheretosend.com");
task.PropertyChanged += (sender, args) => 
{
    if (task.Status != nameof(IHttpStatus.Task))
        return;

    if (task.Status == TaskStatus.Completed)
    {
        task.LocalFilePath // move this file appropriately here
    }
};

```
