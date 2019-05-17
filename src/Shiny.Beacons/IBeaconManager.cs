using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Beacons
{
    public interface IBeaconManager
    {
        /// <summary>
        /// Current status of beacon hardware
        /// </summary>
        AccessState GetCurrentStatus(bool forMonitoring);


        /// <summary>
        /// Observes changes in the access state
        /// </summary>
        /// <returns></returns>
        IObservable<AccessState> WhenAccessStatusChanged(bool monitoring);


        /// <summary>
        /// Request necessary permissions to beacon scanning
        /// </summary>
        /// <returns></returns>
        Task<AccessState> RequestAccess(bool monitoring);


        /// <summary>
        /// Current set of geofences being monitored
        /// </summary>
        Task<IEnumerable<BeaconRegion>> GetMonitoredRegions();


        /// <summary>
        /// Engage the beacon ranging observable
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        IObservable<Beacon> WhenBeaconRanged(BeaconRegion region);

        /// <summary>
        /// Initiates a background beacon region monitor
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        Task StartMonitoring(BeaconRegion region);


        /// <summary>
        /// Stops monitoring a beacon region
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        Task StopMonitoring(BeaconRegion region);


        /// <summary>
        /// Stops all monitoring of beacon regions
        /// </summary>
        /// <returns></returns>
        Task StopAllMonitoring();
    }
}