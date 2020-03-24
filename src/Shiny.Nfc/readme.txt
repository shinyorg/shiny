-----------------
Shiny.Nfc
-----------------

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
        services.UseNfc<YourNfcDelegate>();
    }
}


-----------------
iOS
-----------------

Info.plist

<key>NFCReaderUsageDescription</key>
<string>Put your friendly message as to why here!</string>

Entitlements.plist

<key>com.apple.developer.nfc.readersession.formats</key>
<array>
    <string>NDEF</string>
</array>

-----------------
Android
-----------------

<uses-permission android:name="android.permission.NFC" />
<uses-feature android:name="android.hardware.nfc" android:required="false" />