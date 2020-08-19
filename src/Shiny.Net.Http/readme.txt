-----------------
Shiny.Net.Http
-----------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects


-----------------
iOS
-----------------

Add the following to your AppDelegate.cs

public override void HandleEventsForBackgroundUrl(UIApplication application, string sessionIdentifier, Action completionHandler)
    => this.ShinyHandleEventsForBackgroundUrl(sessionIdentifier, completionHandler);


-----------------
Android
-----------------

Permissions are included via assembly attributes

-----------------
UWP
-----------------