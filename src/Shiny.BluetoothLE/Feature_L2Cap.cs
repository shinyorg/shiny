//using System;
//using System.IO;
//using System.Reactive;
//using System.Reactive.Linq;

//namespace Shiny.BluetoothLE;


//public record L2CapChannel(
//    ushort Psm,
//    Func<byte[], IObservable<Unit>> Write,
//    IObservable<byte[]> DataReceived,
//    Action? OnDispose = null
//) : IDisposable
//{
//    public void Dispose()
//    {
//        this.OnDispose?.Invoke();
//    }
//}


//public interface ICanL2Cap : IPeripheral
//{
//    /// <summary>
//    /// Open an L2Cap socket
//    /// </summary>
//    /// <param name="psm">PSM Value</param>
//    /// <param name="secure">Only applies to Android</param>
//    /// <returns></returns>
//    IObservable<L2CapChannel> OpenL2CapChannel(ushort psm, bool secure);
//}


//public static class FeatureL2Cap
//{
//    public static bool IsL2CapAvailable(this IPeripheral peripheral) => peripheral is ICanL2Cap;


//    public static IObservable<L2CapChannel?> TryOpenL2CapChannel(this IPeripheral peripheral, ushort psm, bool secure)
//    {
//        if (peripheral is ICanL2Cap support)
//            return support.OpenL2CapChannel(psm, secure);

//        return Observable.Empty<L2CapChannel>();
//    }
//}
