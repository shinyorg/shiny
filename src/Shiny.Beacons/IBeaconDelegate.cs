using System;
using System.Threading.Tasks;


namespace Shiny.Beacons
{
    public interface IBeaconDelegate : IShinyDelegate
    {
        Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
    }
}
