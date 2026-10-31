using System;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE;


/// <summary>
/// Optional manager capability that hands out the platform object behind the manager, for diagnostics and
/// inspection tools that need one shape on every platform. Reading it never creates the native object.
/// </summary>
public interface INativeBleManager : IBleManager
{
    /// <summary>
    /// The platform object behind the manager, or null when it has not been created yet:
    /// <c>CBCentralManager</c> on Apple (null until Bluetooth is first used), <c>BluetoothManager</c> on Android,
    /// the selected <c>BluetoothAdapter</c> on Windows (null until a radio has been resolved), and the BlueZ
    /// adapter object path (e.g. <c>/org/bluez/hci0</c>) on Linux (null until the bus is first used).
    /// </summary>
    object? NativeObject { get; }

    /// <summary>
    /// Raised with the new <see cref="NativeObject"/> when the platform creates it: the first time Bluetooth is used
    /// on Apple and Linux, and each time a radio is selected on Windows. Never raised on Android, where the
    /// <c>BluetoothManager</c> exists from construction. Prefer <see cref="FeatureNative.WhenNativeObjectLoaded"/>,
    /// which also emits an object that already exists.
    /// </summary>
    event EventHandler<object>? NativeObjectLoaded;

    /// <summary>
    /// Replaces the platform object Shiny would create with one created by the caller, then raises
    /// <see cref="NativeObjectLoaded"/>. It must be called before the manager is used - before the first scan,
    /// connection or access request - and throws <see cref="InvalidOperationException"/> after that.
    /// <para>
    /// Accepts a <c>CBCentralManager</c> on Apple (Shiny becomes its delegate), a <c>BluetoothManager</c> on Android,
    /// a <c>BluetoothAdapter</c> on Windows (it must support Low Energy), and on Linux a BlueZ adapter object path
    /// string (e.g. <c>/org/bluez/hci1</c>) or an already connected system bus <c>DBusConnection</c>.
    /// Anything else throws <see cref="ArgumentException"/>.
    /// </para>
    /// <para>
    /// <b>Advanced - only with deep knowledge of the platform Bluetooth stack.</b> Shiny assumes it owns the native
    /// object: an object configured differently from what Shiny would have created (another delegate or callback,
    /// dispatch queue, restore identifier, connection or adapter) can silently break scanning, connections, background
    /// restoration and delegates. This is not supported as a general configuration mechanism.
    /// </para>
    /// </summary>
    /// <param name="native">The platform object to use.</param>
    /// <param name="cancelToken">Cancels the lookup some platforms need (the Windows radio).</param>
    Task SetNativeObjectAsync(object native, CancellationToken cancelToken = default);
}


/// <summary>
/// Optional peripheral capability that hands out the platform object behind the peripheral, for diagnostics and
/// inspection tools that need one shape on every platform.
/// </summary>
public interface INativePeripheral : IPeripheral
{
    /// <summary>
    /// The platform object behind the peripheral: <c>CBPeripheral</c> on Apple, <c>BluetoothDevice</c> on Android,
    /// <c>BluetoothLEDevice</c> on Windows (null once the connection is cancelled), and the BlueZ device object
    /// path (e.g. <c>/org/bluez/hci0/dev_AA_BB_CC_DD_EE_FF</c>) on Linux.
    /// </summary>
    object? NativeObject { get; }
}


/// <summary>
/// Helpers for working with the optional <see cref="INativeBleManager"/> and <see cref="INativePeripheral"/> capabilities.
/// </summary>
public static class FeatureNative
{
    /// <summary>
    /// Returns the platform object behind the manager, or null when the platform has none (Blazor WebAssembly)
    /// or it has not been created yet.
    /// </summary>
    /// <param name="manager">The manager to inspect.</param>
    public static object? GetNativeObject(this IBleManager manager) => (manager as INativeBleManager)?.NativeObject;


    /// <summary>
    /// Emits the platform object behind the manager straight away if it already exists, then again whenever the
    /// platform creates (or, on Windows, replaces) it. Completes immediately when the platform has none (Blazor WebAssembly).
    /// Subscribing never creates the native object.
    /// </summary>
    /// <param name="manager">The manager to watch.</param>
    public static IObservable<object> WhenNativeObjectLoaded(this IBleManager manager) => Observable.Create<object>(ob =>
    {
        if (manager is not INativeBleManager native)
        {
            ob.OnCompleted();
            return () => { };
        }

        var handler = new EventHandler<object>((_, obj) => ob.OnNext(obj));
        // hook before reading, so an object created in between is still seen (DistinctUntilChanged drops the repeat)
        native.NativeObjectLoaded += handler;
        if (native.NativeObject is { } current)
            ob.OnNext(current);

        return () => native.NativeObjectLoaded -= handler;
    })
    .DistinctUntilChanged();


    /// <summary>
    /// Replaces the platform object behind the manager - see <see cref="INativeBleManager.SetNativeObjectAsync"/>
    /// for what each platform accepts and when it may be called.
    /// <para>
    /// <b>Advanced - only with deep knowledge of the platform Bluetooth stack.</b> Shiny assumes it owns the native
    /// object: an object configured differently from what Shiny would have created (another delegate or callback,
    /// dispatch queue, restore identifier, connection or adapter) can silently break scanning, connections, background
    /// restoration and delegates. This is not supported as a general configuration mechanism.
    /// </para>
    /// </summary>
    /// <param name="manager">The manager to configure.</param>
    /// <param name="native">The platform object to use.</param>
    /// <param name="cancelToken">Cancels the lookup some platforms need (the Windows radio).</param>
    /// <exception cref="NotSupportedException">The platform has no native object (Blazor WebAssembly).</exception>
    public static Task SetNativeObjectAsync(this IBleManager manager, object native, CancellationToken cancelToken = default)
    {
        if (manager is not INativeBleManager nativeManager)
            throw new NotSupportedException("This platform has no native Bluetooth manager to replace");

        return nativeManager.SetNativeObjectAsync(native, cancelToken);
    }


    /// <summary>
    /// Returns the platform object behind the peripheral, or null when the platform has none (Blazor WebAssembly).
    /// </summary>
    /// <param name="peripheral">The peripheral to inspect.</param>
    public static object? GetNativeObject(this IPeripheral peripheral) => (peripheral as INativePeripheral)?.NativeObject;


    /// <summary>
    /// Returns the platform object behind the manager as <typeparamref name="T"/>, or null when it is not one.
    /// </summary>
    /// <typeparam name="T">The expected platform type, e.g. <c>CBCentralManager</c>.</typeparam>
    /// <param name="manager">The manager to inspect.</param>
    public static T? GetNativeObject<T>(this IBleManager manager) where T : class => manager.GetNativeObject() as T;


    /// <summary>
    /// Returns the platform object behind the peripheral as <typeparamref name="T"/>, or null when it is not one.
    /// </summary>
    /// <typeparam name="T">The expected platform type, e.g. <c>CBPeripheral</c>.</typeparam>
    /// <param name="peripheral">The peripheral to inspect.</param>
    public static T? GetNativeObject<T>(this IPeripheral peripheral) where T : class => peripheral.GetNativeObject() as T;
}
