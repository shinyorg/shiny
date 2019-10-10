using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Peripherals
{
    public interface IPeripheralManager
    {
        AccessState Status { get; }
        IObservable<AccessState> WhenStatusChanged();

        bool IsAdvertising { get; }
        Task StartAdvertising(AdvertisementData adData = null);
        Task StartAdvertising(AdvertisementData adData = null, AdvertisementData scanrespadData = null);
        void StopAdvertising();

        Task<IGattService> AddService(Guid uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder);
        void RemoveService(Guid serviceUuid);
        void ClearServices();

        IReadOnlyList<IGattService> Services { get; }
    }
}
