using System;
using System.Reactive.Linq;
using Shiny.Net;

namespace Shiny;


/// <summary>
/// Convenience extensions over <see cref="IConnectivity"/>.
/// </summary>
public static class ConnectivityExtensions
{
    /// <summary>
    /// Returns true if the device currently has internet access.
    /// </summary>
    /// <param name="connectivity">The connectivity service.</param>
    /// <param name="allowConstrained">When true, constrained internet (e.g. Low Data Mode) counts as available.</param>
    public static bool IsInternetAvailable(this IConnectivity connectivity, bool allowConstrained = true)
        => connectivity.Access.HasFlag(NetworkAccess.Internet) || (allowConstrained && connectivity.Access.HasFlag(NetworkAccess.ConstrainedInternet));


    /// <summary>
    /// Emits the current internet-available state on subscription and again whenever it changes.
    /// </summary>
    /// <param name="connectivity">The connectivity service.</param>
    /// <param name="allowConstrained">When true, constrained internet counts as available.</param>
    public static IObservable<bool> WhenInternetStatusChanged(this IConnectivity connectivity, bool allowConstrained = true) => connectivity
        .WhenChanged()
        .Select(x => x.IsInternetAvailable(allowConstrained))
        .StartWith(connectivity.IsInternetAvailable(allowConstrained))
        .DistinctUntilChanged();
}
