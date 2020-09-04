using System;
using System.Threading.Tasks;


namespace Shiny.Beacons.Advertising
{
    public interface IBeaconAdvertiser
    {
        Guid? Uuid { get; }
        ushort? Major { get; }
        ushort? Minor { get; }
        int? TxPower { get; }

        Task Start(Guid uuid, ushort major, ushort minor, int txpower = 0);
        Task Stop();
    }
}
