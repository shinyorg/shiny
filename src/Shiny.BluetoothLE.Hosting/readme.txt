-------------------------
Shiny.BluetoothLE.Hosting
-------------------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------
Shiny Startup
-----------------
public class YourShinyStartup : Shiny.ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseBleHosting();
    }
}


-----------------
iOS
-----------------
<key>NSBluetoothPeripheralUsageDescription</key>
<string>Example</string> 
<key>NSBluetoothAlwaysUsageDescription</key>
<string>Example</string>
<key>UIBackgroundModes</key>
<array>
	<string>bluetooth-peripheral</string>
</array>


-----------------
Android
-----------------

All Android permissions will be included automagically

-----------------
UWP
-----------------

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="bluetooth" />
    </BackgroundTasks>
</Extension>