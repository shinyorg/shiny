using Shiny.BluetoothLE.Hosting;

namespace Sample.BleHosting;


public class ManagedViewModel : ViewModel
{
    public ManagedViewModel(BaseServices services, IBleHostingManager hostingManager) : base(services)
    {
        this.IsBroadcasting = hostingManager.IsRegisteredServicesAttached;

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
            this.IsBroadcasting = hostingManager.IsRegisteredServicesAttached;
        });
    }

    public ICommand Toggle { get; }

    [Reactive] public string BroadcastLocalName { get; set; }
    [Reactive] public bool IsBroadcasting { get; set; }
    public string ServiceUuid => Constants.ManagedServiceUuid;
    public string CharacteristicUuid => Constants.ManagedCharacteristicUuid;
}

