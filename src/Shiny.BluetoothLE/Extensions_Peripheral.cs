using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
    /// Attempts to connect if not already connected
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="config"></param>
    /// <returns>A completing observable</returns>
    public static IObservable<IPeripheral> WithConnectIf(this IPeripheral peripheral, ConnectionConfig? config = null) => Observable.Create<IPeripheral>(ob =>
    {
        if (peripheral.Status == ConnectionState.Connected)
        {
            ob.Respond(peripheral);
            return Disposable.Empty; // we didn't start the connection, so we aren't closing it
        }

        var comp = new CompositeDisposable();
        peripheral
            .WhenConnected()
            .Take(1)
            .Subscribe(_ => ob.Respond(peripheral))
            .DisposedBy(comp);

        peripheral
            .WhenConnectionFailed()
            .Subscribe(ob.OnError)
            .DisposedBy(comp);

        peripheral.Connect(config);

        return Disposable.Create(() =>
        {
            comp.Dispose();
            if (peripheral.Status != ConnectionState.Connected)
                peripheral.CancelConnection();
        });
    });

    
    /// <summary>
    /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Connected)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns>A streaming observable - this will pump connected if subscribing when connected</returns>
    public static IObservable<IPeripheral> WhenConnected(this IPeripheral peripheral) =>
        peripheral
            .WhenStatusChanged()
            .Where(x => x == ConnectionState.Connected)
            .Select(_ => peripheral);

    
    /// <summary>
    /// Quick helper around WhenStatusChanged().Where(x => x == ConnectionStatus.Disconnected)
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns>A streaming observable - will bump disconnected if hooking when already disconnected</returns>
    public static IObservable<IPeripheral> WhenDisconnected(this IPeripheral peripheral) =>
        peripheral
            .WhenStatusChanged()
            .Where(x => x == ConnectionState.Disconnected)
            .Select(_ => peripheral);

    
    /// <summary>
    /// Hooks a notification subscription - NOTE: this is a refcount observable. A characteristic will stay hooked
    /// as long as their are subscribers
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <param name="useIndicationsIfAvailable"></param>
    /// <returns>A streaming observable</returns>
    public static IObservable<BleCharacteristicResult> NotifyCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, bool useIndicationsIfAvailable = true)
        => peripheral.NotifyCharacteristic(info.Service.Uuid, info.Uuid, useIndicationsIfAvailable);

    
    /// <summary>
    /// Read a characteristic value
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <returns>A completing observable</returns>
    public static IObservable<BleCharacteristicResult> ReadCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info)
        => peripheral.ReadCharacteristic(info.Service.Uuid, info.Uuid);
    
    
    /// <summary>
    /// Writes a value to a characteristic
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <param name="data"></param>
    /// <param name="withResponse"></param>
    /// <returns>A completing observable</returns>
    public static IObservable<BleCharacteristicResult> WriteCharacteristic(this IPeripheral peripheral, BleCharacteristicInfo info, byte[] data, bool withResponse = true)
        => peripheral.WriteCharacteristic(info.Service.Uuid, info.Uuid, data, withResponse);

    
    /// <summary>
    /// Get all descriptors
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <returns>A completing observable</returns>
    public static IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(this IPeripheral peripheral, BleCharacteristicInfo info)
        => peripheral.GetDescriptors(info.Service.Uuid, info.Uuid);

    
    /// <summary>
    /// Write a value to a descriptor
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <param name="data"></param>
    /// <returns>A completing observable</returns>
    public static IObservable<BleDescriptorResult> WriteDescriptor(this IPeripheral peripheral, BleDescriptorInfo info, byte[] data)
        => peripheral.WriteDescriptor(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid, data);

    
    /// <summary>
    /// Read a descriptor value
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="info"></param>
    /// <returns>A completing observable with the byte array from the read operation</returns>
    public static IObservable<BleDescriptorResult> ReadDescriptor(this IPeripheral peripheral, BleDescriptorInfo info)
        => peripheral.ReadDescriptor(info.Characteristic.Service.Uuid, info.Characteristic.Uuid, info.Uuid);
}