using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Hosting
{
    public interface IBleHostingManager
    {
        AccessState Status { get; }
        IObservable<AccessState> WhenStatusChanged();
        IObservable<L2CapChannel> WhenL2CapChannelOpened(bool secure);

        bool IsAdvertising { get; }
        Task StartAdvertising(AdvertisementOptions? options = null);
        void StopAdvertising();

        Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
        void RemoveService(string serviceUuid);
        void ClearServices();

        IReadOnlyList<IGattService> Services { get; }
    }
}
