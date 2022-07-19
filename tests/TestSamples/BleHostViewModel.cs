using Shiny.BluetoothLE.Hosting;

namespace TestSamples;


public class BleHostViewModel : ReactiveObject
{
    public BleHostViewModel(IBleHostingManager bleHost)
    {
    }


    public ICommand StartServer { get; }
    public ICommand StopServer { get; }
}
