using Shiny.Net;

namespace Shiny;


public static class ConnectivityExtensions
{
    public static bool IsInternetAvailable(this IConnectivity connectivity, bool allowConstrained = true)
        => connectivity.Access == NetworkAccess.Internet || (allowConstrained && connectivity.Access == NetworkAccess.ConstrainedInternet);
}
