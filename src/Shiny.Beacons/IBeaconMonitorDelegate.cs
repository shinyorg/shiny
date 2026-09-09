using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Receives beacon region enter and exit transitions, including while the app is backgrounded or
/// has been restarted by the OS.
/// </summary>
/// <remarks>
/// Register an implementation through <c>AddBeaconMonitoring&lt;T&gt;()</c>. Implementations are
/// resolved from a fresh scope each time a transition fires, so keep constructor dependencies light.
/// </remarks>
public interface IBeaconMonitorDelegate
{
    /// <summary>
    /// Called when a monitored region is entered or left.
    /// </summary>
    /// <param name="newStatus">The state the region moved into.</param>
    /// <param name="region">The region that changed.</param>
    Task OnStatusChanged(BeaconRegionState newStatus, BeaconRegion region);
}
