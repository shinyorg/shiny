using System;
using System.Threading.Tasks;
using ExposureNotifications;


namespace Shiny.ExposureNotifications
{
    public class ExposureNotificationManager : IExposureNotificationManager
    {
        readonly ENManager manager;


        public ExposureNotificationManager()
        {
            this.manager = new ENManager();
        }


        public async Task<AccessState> StartMonitoring()
        {
            //this.manager.DetectExposuresAsync()
            // ExposureManager.shared.showBluetoothOffUserNotificationIfNeeded()
            // TODO: use push notifications here
            if (this.manager.ExposureNotificationStatus == ENStatus.Unknown)
                await this.manager.ActivateAsync();
            
            return this.manager.ExposureNotificationStatus switch
            {
                ENStatus.Active => AccessState.Available,
                ENStatus.BluetoothOff => AccessState.Disabled,
                ENStatus.Disabled => AccessState.Disabled,
                ENStatus.Restricted => AccessState.Restricted,
                ENStatus.Unknown => AccessState.Unknown,
                _ => AccessState.Unknown
            };
        }
    }
}
