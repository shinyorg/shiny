using Sample.MacOS.Pages.BLE;

namespace Sample.MacOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("blescan", typeof(BleScanPage));
        Routing.RegisterRoute("bleperipheral", typeof(BlePeripheralPage));
        Routing.RegisterRoute("blecharacteristic", typeof(BleCharacteristicPage));
    }
}
