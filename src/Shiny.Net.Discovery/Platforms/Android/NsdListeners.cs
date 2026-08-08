using Android.Net.Nsd;

namespace Shiny.Net.Discovery;


/// <summary>
/// NsdManager is callback-per-interface rather than event based, so each listener is a thin
/// adapter onto delegates the manager supplies.
/// </summary>
class NsdDiscoveryListener(
    Action<NsdServiceInfo> onFound,
    Action<NsdServiceInfo> onLost,
    Action<string, NsdFailure> onFailed
) : Java.Lang.Object, NsdManager.IDiscoveryListener
{
    public void OnDiscoveryStarted(string? serviceType) { }

    public void OnDiscoveryStopped(string? serviceType) { }

    public void OnServiceFound(NsdServiceInfo? serviceInfo)
    {
        if (serviceInfo != null)
            onFound(serviceInfo);
    }

    public void OnServiceLost(NsdServiceInfo? serviceInfo)
    {
        if (serviceInfo != null)
            onLost(serviceInfo);
    }

    public void OnStartDiscoveryFailed(string? serviceType, NsdFailure errorCode)
        => onFailed(serviceType ?? String.Empty, errorCode);

    public void OnStopDiscoveryFailed(string? serviceType, NsdFailure errorCode) { }
}


class NsdResolveListener(Action<NsdServiceInfo> onResolved, Action<NsdFailure> onFailed)
    : Java.Lang.Object, NsdManager.IResolveListener
{
    public void OnResolveFailed(NsdServiceInfo? serviceInfo, NsdFailure errorCode)
        => onFailed(errorCode);

    public void OnServiceResolved(NsdServiceInfo? serviceInfo)
    {
        if (serviceInfo == null)
            onFailed(NsdFailure.InternalError);
        else
            onResolved(serviceInfo);
    }
}


/// <summary>
/// The API 34+ replacement for IResolveListener. Unlike the old resolve it streams updates and
/// several may be in flight at once, so no serialisation is needed on this path.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("android34.0")]
class NsdServiceInfoCallback(Action<NsdServiceInfo> onUpdated, Action<NsdFailure> onFailed)
    : Java.Lang.Object, NsdManager.IServiceInfoCallback
{
    public void OnServiceInfoCallbackRegistrationFailed(NsdFailure errorCode) => onFailed(errorCode);

    public void OnServiceUpdated(NsdServiceInfo? serviceInfo)
    {
        if (serviceInfo != null)
            onUpdated(serviceInfo);
    }

    public void OnServiceLost() { }

    public void OnServiceInfoCallbackUnregistered() { }
}


class NsdRegistrationListener(Action<NsdServiceInfo> onRegistered, Action<NsdFailure> onFailed)
    : Java.Lang.Object, NsdManager.IRegistrationListener
{
    public void OnRegistrationFailed(NsdServiceInfo? serviceInfo, NsdFailure errorCode)
        => onFailed(errorCode);

    public void OnServiceRegistered(NsdServiceInfo? serviceInfo)
    {
        if (serviceInfo == null)
            onFailed(NsdFailure.InternalError);
        else
            onRegistered(serviceInfo);
    }

    public void OnServiceUnregistered(NsdServiceInfo? serviceInfo) { }

    public void OnUnregistrationFailed(NsdServiceInfo? serviceInfo, NsdFailure errorCode) { }
}
