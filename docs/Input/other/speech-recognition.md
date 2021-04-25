Title: Speech Recognition
---

## USAGE

|Area|Info|
|----|----|
|NuGet| [![SpeechRecognitionNugetShield]][SpeechRecognitionNuget] |
|Shiny Startup|services.UseSpeechRecognition|
|Main Service|Shiny.SpeechRecognition.ISpeechRecognizer|
|Shiny Delegate|None|
|Static Generated|ShinySpeechRecognizer|
|Manual Resolve|ShinyHost.Resolve<Shiny.SpeechRecognition.ISpeechRecognizer>()|
|Xamarin.Forms|DependencyService.Get<Shiny.SpeechRecognition.ISpeechRecognizer>()|


## HOW TO


#### iOS
Add the following to your 
```xml
<key>NSSpeechRecognitionUsageDescription</key>  
<string>Say something useful here</string>  
<key>NSMicrophoneUsageDescription</key>  
<string>Say something useful here</string> 
```

#### Android

Add the following to your AndroidManifest.xml
```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
```

#### UWP 
Add the following to your app manifest
```xml
<Capabilities>
	<Capability Name="internetClient" />
 	<DeviceCapability Name="microphone" />
</Capabilities>
```

## HOW TO USE

### Request Permission
```csharp
var granted = await CrossSpeechRecognition.Current.RequestPermission();
if (granted) 
{
    // go!
}
```

### Continuous Dictation
```csharp
var listener = CrossSpeechRecognition
    .Current
    .ContinuousDictation()
    .Subscribe(phrase => {
        // will keep returning phrases as pause is observed
    });

// make sure to dispose to stop listening
listener.Dispose();

```


### Listen for a phrase (good for a web search)
```csharp
CrossSpeechRecognition
    .Current
    .ListenUntilPause()
    .Subscribe(phrase => {})
```

### Listen for keywords
```csharp
CrossSpeechRecognition
    .Current
    .ListenForKeywords("yes", "no")
    .Subscribe(firstKeywordHeard => {})
```


### When can I talk?
```csharp
CrossSpeechRecognition
    .Current
    .WhenListenStatusChanged()
    .Subscribe(isListening => { you can talk if this is true });
```
    
## FAQ

Q. Why use reactive extensions and not async?

A. Speech is very event stream oriented which fits well with RX


Q. Should I use CrossSpeechRecognition.Current?

A. Hell NO!  DI that sucker using the Instance


## Roadmap

* Multilingual
* Confidence Scoring
* Mac Support
* Start and end of speech eventing
* RMS detection
