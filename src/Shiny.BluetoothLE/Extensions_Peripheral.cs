using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Shiny.BluetoothLE;


public static class PeripheralExtensions
{
    /// <summary>
    /// Quick access method for checking if device is connected instead of looking at Status enum
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static bool IsConnected(this IPeripheral peripheral)
        => peripheral.Status == ConnectionState.Connected;
    
    
    /// <summary>
    /// Starts connection process if not already connecteds
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="config"></param>
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
            return Disposable.Empty; // we didn't start the connection, so we aren't closing it
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


    public static IObservable<BleCharacteristicResult> NotifyCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, bool useIndicationsIfAvailable = true)
        => peripheral.NotifyCharacteristic(info.Service.Uuid, info.Uuid, useIndicationsIfAvailable);

    public static IObservable<BleCharacteristicResult> ReadCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info)
        => peripheral.ReadCharacteristic(info.Service.Uuid, info.Uuid);

    public static IObservable<BleCharacteristicResult> WriteCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, byte[] data, bool withoutResponse = false)
        => peripheral.WriteCharacteristic(info.Service.Uuid, info.Uuid, data, withoutResponse);

    public static IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(this IPeripheral peripheral, BleCharacteristicInfo info)
        => peripheral.GetDescriptors(info.Service.Uuid, info.Uuid);

    public static IObservable<BleDescriptorResult> WriteDescriptor(this IPeripheral peripheral, BleDescriptorInfo info, byte[] data)
        => peripheral.WriteDescriptor(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid, data);

    public static IObservable<BleDescriptorResult> ReadDescriptor(this IPeripheral peripheral, BleDescriptorInfo info)
        => peripheral.ReadDescriptor(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid);
}