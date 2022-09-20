using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class ManagedViewModel : ViewModel
{
    public ManagedViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.Toggle = ReactiveCommand.CreateFromTask(async () =>
        {
            if (hostingManager.IsRegisteredServicesAttached)
            {
                hostingManager.DetachRegisteredServices();
            }
            else
            {
                await hostingManager.AttachRegisteredServices();
            }
        });
    }

    public ICommand Toggle { get; }
}

