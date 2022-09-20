namespace Sample.Platform;


public class ConnectivityViewModel : ViewModel
{
    public ConnectivityViewModel(BaseServices services, Shiny.Net.IConnectivity connectivity) : base(services)
    {
        connectivity
            .WhenChanged()
            .SubOnMainThread(_ =>
            {
                this.Types = connectivity.ConnectionTypes;
                this.Access = connectivity.Access;
            })
            .DisposedBy(this.DestroyWith);
    }


    [Reactive] public Shiny.Net.NetworkAccess Access { get; private set; }
    [Reactive] public Shiny.Net.ConnectionTypes Types { get; private set; }
}

