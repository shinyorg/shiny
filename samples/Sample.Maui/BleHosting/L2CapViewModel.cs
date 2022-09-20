using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class L2CapViewModel : ViewModel
{
    public L2CapViewModel(BaseServices services, IBleHostingManager bleManager) : base(services)
    {
    }
}
