Title: Android
Description: Working with Android
Category: Platform
---
To use this library, take these steps...

```csharp
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    => AndroidShinyHost.OnRequestPermissionsResult(requestCode, permissions, grantResults);

```