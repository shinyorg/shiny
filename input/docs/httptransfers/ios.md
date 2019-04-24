Title: iOS
---

Add the following to your AppDelegate.cs

```csharp
public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
    => HttpTransferManager.SetCompletionHandler(sessionIdentifier, completionHandler);
```