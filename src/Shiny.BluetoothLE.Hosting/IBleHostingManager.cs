using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Hosting
{
    public interface IBleHostingManager
    {
        Task<AccessState> RequestAccess(bool advertise = true, bool gattConnect = true);

        bool IsAdvertising { get; }
        Task StartAdvertising(AdvertisementOptions? options = null);
        void StopAdvertising();

        Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
        void RemoveService(string serviceUuid);
        void ClearServices();

        IReadOnlyList<IGattService> Services { get; }
    }
}
