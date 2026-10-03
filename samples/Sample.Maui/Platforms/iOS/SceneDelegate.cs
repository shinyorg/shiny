using Foundation;

namespace Sample.Maui;

// iOS 27 terminates apps that have not adopted the UIScene lifecycle; UIApplicationSceneManifest in Info.plist names this
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate;
