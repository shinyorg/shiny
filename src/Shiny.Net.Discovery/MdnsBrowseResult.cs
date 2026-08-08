namespace Shiny.Net.Discovery;


/// <summary>
/// Why a browse result was emitted.
/// </summary>
public enum MdnsBrowseStatus
{
    /// <summary>
    /// The service appeared on the link. A service that re-announces itself (eg after the
    /// publisher restarts, or when its records are refreshed) is emitted as Found again -
    /// treat this as an upsert keyed on <see cref="MdnsService.FullName"/>.
    /// </summary>
    Found,

    /// <summary>
    /// The service went away, either by sending a goodbye packet or by its records expiring.
    /// Only <see cref="MdnsService.InstanceName"/>, <see cref="MdnsService.ServiceType"/> and
    /// <see cref="MdnsService.Domain"/> are guaranteed to be populated on a Lost result.
    /// </summary>
    Lost
}


/// <summary>
/// A single change emitted by <see cref="IMdnsManager.Browse(MdnsBrowseConfig, CancellationToken)"/>.
/// </summary>
/// <param name="Status">Whether the service appeared or went away.</param>
/// <param name="Service">The service the change applies to.</param>
public sealed record MdnsBrowseResult(MdnsBrowseStatus Status, MdnsService Service);
