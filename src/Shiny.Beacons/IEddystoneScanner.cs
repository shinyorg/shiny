using System;
using System.Threading.Tasks;

namespace Shiny.Beacons;


/// <summary>
/// Reports Eddystone frames seen on the air.
/// </summary>
/// <remarks>
/// Eddystone rides in BLE service data under UUID 0xFEAA, which every platform surfaces through a
/// normal scan - including Apple's, where iBeacon is hidden. That makes this the one beacon format
/// that behaves the same everywhere.
/// </remarks>
public interface IEddystoneScanner
{
    /// <summary>
    /// The current permission state, without prompting the user.
    /// </summary>
    AccessState CurrentStatus { get; }

    /// <summary>
    /// Requests the permissions scanning needs.
    /// </summary>
    /// <returns>The resulting access state once the user has responded.</returns>
    Task<AccessState> RequestAccess();

    /// <summary>
    /// Emits every Eddystone frame received while subscribed.
    /// </summary>
    /// <returns>
    /// An observable of frames. Scanning starts on first subscription and stops when the last
    /// subscription is disposed.
    /// </returns>
    IObservable<EddystoneFrame> WhenFrameReceived();
}
