using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE;


public static class PeripheralExtensions
{
    /// <summary>
    /// Starts connection process if not already connecteds
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="connectionConfig"></param>
    /// <returns>True if connection attempt was sent, otherwise false</returns>
    public static bool ConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null)
    {
        if (peripheral.Status == ConnectionState.Disconnected)
        {
            peripheral.Connect(config);
            return true;
        }
        return false;
    }


    /// <summary>
    /// Attempts to connect if not already connected
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> WithConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null) => Observable.Create<IPeripheral>(ob =>
    {
        if (peripheral.Status == ConnectionState.Connected)
        {
            ob.Respond(peripheral);
            return Disposable.Empty;
        }

        var sub1 = peripheral
            .WhenConnected()
            .Take(1)
            .Subscribe(_ => ob.Respond(peripheral));

        // TODO: watch for new failed state?
        //var sub2 = peripheral
        //    .WhenConnectionFailed()
        //    .Subscribe(ob.OnError);

        peripheral.Connect(config);

        return Disposable.Create(() =>
        {
            sub1.Dispose();
            //sub2.Dispose();
            if (peripheral.Status != ConnectionState.Connected)
                peripheral.CancelConnection();
        });
    });


    /// <summary>
    /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> WhenConnected(this IPeripheral peripheral) =>
        peripheral
            .WhenStatusChanged()
            .Where(x => x == ConnectionState.Connected)
            .Select(_ => peripheral);


    /// <summary>
    /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Disconnected)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static IObservable<IPeripheral> WhenDisconnected(this IPeripheral peripheral) =>
        peripheral
            .WhenStatusChanged()
            .Where(x => x == ConnectionState.Disconnected)
            .Select(_ => peripheral);
}