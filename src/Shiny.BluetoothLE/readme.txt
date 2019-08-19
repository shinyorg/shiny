-----------------
Shiny.BluetoothLE
-----------------

GitHub: https://github.com/shinyorg/Shiny
Samples: https://github.com/shinyorg/ShinySamples
Docs: https://shinylib.net
Blog: https://allancritchie.net

Please star this project on GitHub if you use it in your projects

-----------------
iOS
-----------------

<key>NSBluetoothPeripheralUsageDescription</key>
<string>Example</string>

<key>UIBackgroundModes</key>
<array>
	<string>bluetooth-central</string>
	<string>bluetooth-peripheral</string>
</array>


-----------------
Android
-----------------

All Android permissions will be included automagically

-----------------
UWP
-----------------

<Extension Category="windows.backgroundTasks" EntryPoint="Shiny.Support.Uwp.ShinyBackgroundTask">
    <BackgroundTasks>
        <Task Type="bluetooth" />
    </BackgroundTasks>
</Extension>