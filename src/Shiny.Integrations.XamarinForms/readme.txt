-------------------------------
Shiny.Integrations.XamarinForms
-------------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

This project contains quick wrappers for FormsAppCompatActivity and FormsApplicationDelegate to glue Shiny & Xamarin Forms together

### iOS

```csharp
public class YourAppDelegate : FormsApplicationDelegate<YourShinyStartup>
{
    public override bool FinishedLaunching(UIApplication app, NSDictionary options)
    {
        // add your stuff just like normal
        return base.FinishedLaunching(app, options);
    }
}
```

### Android

```csharp

public class YourMainFormsActivity : ShinyFormsActivity
{
}
```