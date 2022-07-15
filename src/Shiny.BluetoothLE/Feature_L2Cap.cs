using System;
using System.IO;

namespace Shiny.BluetoothLE;


public record L2CapChannel(
    ushort Psm,
    Stream OutputStream,
    Stream InputStream,
    Action? OnDispose = null
) : IDisposable
{
    public void Dispose()
    {
        this.OnDispose?.Invoke();
    }
}


public interface IL2CapSupport : IPeripheral
{
    /// <summary>
    /// Open an L2Cap socket
    /// </summary>
    /// <param name="psm">PSM Value</param>
    /// <returns></returns>
    IObservable<L2CapChannel> OpenL2CapChannel(ushort psm);
}

public static class FeatureL2Cap
{
    public static bool IsL2CapAvailable(this IPeripheral peripheral) => peripheral is IL2CapSupport;


    public static IObservable<L2CapChannel>? TryOpenL2CapChannel(this IPeripheral peripheral, ushort psm)
    {
        if (peripheral is IL2CapSupport support)
            return support.OpenL2CapChannel(psm);

        return null;
    }
}
