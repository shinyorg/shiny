using System;
using System.Reactive.Subjects;
using Microsoft.JSInterop;

namespace Shiny.BluetoothLE;


/// <summary>
/// The JS-invokable bridge for a single <see cref="Peripheral"/>: carries GATT notifications and
/// disconnect events back from ble.js.
/// </summary>
public class PeripheralCallback : IDisposable
{
    readonly Subject<(string Service, string Characteristic, byte[] Data)> notifications = new();
    readonly Subject<bool> connectionState = new();

    /// <summary>Emits each notification/indication payload received from the peripheral.</summary>
    public IObservable<(string Service, string Characteristic, byte[] Data)> WhenNotification() => this.notifications;

    /// <summary>Emits false when the browser reports the GATT server has disconnected.</summary>
    public IObservable<bool> WhenConnectionStateChanged() => this.connectionState;


    /// <summary>Invoked from ble.js. Payloads arrive base64-encoded for reliable JS to .NET transfer.</summary>
    [JSInvokable("OnNotification")]
    public void OnNotification(string serviceUuid, string characteristicUuid, string base64)
        => this.notifications.OnNext((serviceUuid, characteristicUuid, Convert.FromBase64String(base64)));


    /// <summary>Invoked from ble.js on the gattserverdisconnected event.</summary>
    [JSInvokable("OnConnectionStateChanged")]
    public void OnConnectionStateChanged(bool connected)
        => this.connectionState.OnNext(connected);


    public void Dispose()
    {
        this.notifications.Dispose();
        this.connectionState.Dispose();
    }
}
