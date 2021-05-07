Title: Firebase Messaging
---

<?! PackageInfo "Shiny.Push.FirebaseMessaging" /?>


## Android
Follow the exact same setup process as the [native push provider](xref:pushnative)

Please read the following [Microsoft Document](https://docs.microsoft.com/en-us/xamarin/android/data-cloud/google-messaging/firebase-cloud-messaging) on
how to setup firebase within your Android application.

## iOS

Follow the exact same setup process as the [native push provider](xref:pushnative) with one addition

After setting up your iOS app within the firebase admin portal, add GoogleService-Info.plist to your iOS head project
and mark it as a "BundleResource"