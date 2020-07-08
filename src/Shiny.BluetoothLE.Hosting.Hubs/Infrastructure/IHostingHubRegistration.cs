using System;


namespace Shiny.BluetoothLE.Hosting.Hubs.Infrastructure
{
    public interface IHostingHubRegistration
    {
        void Register(IBleHostingManager manager, Type[] hubTypes);
    }
}
