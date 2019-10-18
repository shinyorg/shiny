using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Beacons
{
    public interface IBeaconDelegate : IShinyDelegate
    {
        Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
    }
}
