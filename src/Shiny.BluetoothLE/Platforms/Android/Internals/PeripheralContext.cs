using System;
using System.Threading;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.OS;
using Android.Bluetooth;
using Exception = System.Exception;
using Microsoft.Extensions.Logging;


namespace Shiny.BluetoothLE.Internals
{
    public class PeripheralContext
    {
        public PeripheralContext(ManagerContext context, BluetoothDevice device)
        {
            this.ManagerContext = context;
            this.NativeDevice = device;
        }


        readonly Subject<BleException> connErrorSubject = new();
        public GattCallbacks Callbacks { get; } = new();

        public ManagerContext ManagerContext { get; }
        public BluetoothGatt? Gatt { get; private set; }
        public BluetoothDevice NativeDevice { get; }


        ILogger? logger;
        ILogger Logger => this.logger ??= this.ManagerContext.Logging.CreateLogger(this.GetType());


        public ConnectionState Status
        {
            get
            {
                if (this.Gatt == null)
                    return ConnectionState.Disconnected;

                return this
                    .ManagerContext
                    .Manager
                    .GetConnectionState(this.NativeDevice, ProfileType.Gatt)
                    .ToStatus();
            }
        }


        public IObservable<BleException> ConnectionFailed => this.connErrorSubject;


        public void Connect(ConnectionConfig? config) => this.InvokeOnMainThread(() =>
        {
            try
            {
                AndroidConnectionConfig cfg = null!;
                if (config == null)
                    cfg = new();
                else if (config is AndroidConnectionConfig cfg1)
                    cfg = cfg1;
                else
                    cfg = new AndroidConnectionConfig(cfg.AutoConnect);

                this.CreateGatt(config?.AutoConnect ?? true);
                if (this.Gatt == null)
                    throw new BleException("GATT connection could not be established");

                this.Gatt.RequestConnectionPriority(cfg.ConnectionPriority);
            }
            catch (Exception ex)
            {
                this.connErrorSubject.OnNext(new BleException("Failed to connect to peripheral", ex));
            }
        });


        readonly SemaphoreSlim semaphore = new(1, 1);

        public IObservable<T> Invoke<T>(IObservable<T> observable)
        {
            if (!this.ManagerContext.Configuration.UseInternalSyncQueue)
                return observable;

            return Observable.FromAsync(async ct =>
            {
                await this.semaphore
                    .WaitAsync(ct)
                    .ConfigureAwait(false);

                try
                {
                    return await observable
                        .ToTask(ct)
                        .ConfigureAwait(false);
                }
                finally
                {
                    this.semaphore.Release();
                }
            });
        }


        readonly Handler handler = new(Looper.MainLooper!);
        public void InvokeOnMainThread(Action action)
        {
            if (this.ManagerContext.Configuration.InvokeCallsOnMainThread)
                this.handler.Post(action);
            else
                action();
        }


        public void Close()
        {
            try
            {
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "BLE Peripheral did not cleanly disconnect");
            }
        }


        public void RefreshServices()
        {
            if (this.Gatt == null || !this.ManagerContext.Configuration.FlushServicesBetweenConnections)
                return;

            // https://stackoverflow.com/questions/22596951/how-to-programmatically-force-bluetooth-low-energy-service-discovery-on-android
            try
            {
                //Log.Warn(BleLogCategory.Device, "Try to clear Android cache");
                var method = this.Gatt.Class.GetMethod("refresh");
                if (method != null)
                {
                    var result = (bool)method.Invoke(this.Gatt);
                    this.Logger.LogWarning("Clear Internal Cache Refresh Result: " + result);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogWarning(ex, "Failed to clear internal device cache");
            }
        }


        void CreateGatt(bool autoConnect) => this.Gatt = this.NativeDevice.ConnectGatt(
            this.ManagerContext.Android.AppContext,
            autoConnect,
            this.Callbacks,
            BluetoothTransports.Le
        );

    }
}

//bool running;
//async void ProcessQueue()
//{
//    if (this.running)
//        return;

//    try
//    {
//        this.running = true;
//        Func<Task>? outTask = null;
//        while (this.Actions.TryDequeue(out outTask) && this.running)
//        {
//            await outTask();
//        }
//    }
//    catch (TaskCanceledException)
//    {
//    }
//    this.running = false;
//}


//void CleanUpQueue()
//{
//    this.running = false;
//    this.cancelSrc?.Cancel();
//    this.cancelSrc = new CancellationTokenSource();
//    this.Actions.Clear();
//}


//void CreateGatt(bool autoConnect)
//try
//{
//    // somewhat a copy of android-rxbluetoothle
//    if (this.CentralContext.Android.IsMinApiLevel(24))
//    {
//        this.Gatt = this.ConnectGattCompat(autoConnect);
//        return;
//    }

//    var bmMethod = BluetoothAdapter.DefaultAdapter.Class.GetDeclaredMethod("getBluetoothManager");
//    bmMethod.Accessible = true;
//    var bluetoothManager = bmMethod.Invoke(BluetoothAdapter.DefaultAdapter);

//    var method = bluetoothManager.Class.GetDeclaredMethod("getBluetoothGatt");
//    method.Accessible = true;
//    var iBluetoothGatt = method.Invoke(bluetoothManager);

//    if (iBluetoothGatt == null)
//    {
//        Log.Write(BleLogCategory.Peripheral, "Unable to find getBluetoothGatt object");
//        this.Gatt = this.ConnectGattCompat(autoConnect);
//        return;
//    }

//    var bluetoothGatt = this.CreateReflectionGatt(iBluetoothGatt);
//    if (bluetoothGatt == null)
//    {
//        Log.Write(BleLogCategory.Peripheral, "Unable to create GATT object via reflection");
//        this.Gatt = this.ConnectGattCompat(autoConnect);
//        return;
//    }

//    this.Gatt = bluetoothGatt;
//    var connectSuccess = this.ConnectUsingReflection(this.Gatt, true);
//    if (!connectSuccess)
//    {
//        Log.Write(BleLogCategory.Peripheral, "Unable to connect using reflection method");
//        this.Gatt?.Close();
//    }
//}
//catch (Exception ex)
//{
//    Log.Write(BleLogCategory.Peripheral, "Defaulting to gatt connect compatible method - " + ex);
//    this.Gatt = this.ConnectGattCompat(autoConnect);
//}
//}


//bool ConnectUsingReflection(BluetoothGatt bluetoothGatt, bool autoConnect)
//{
//    if (autoConnect)
//    {
//        var autoConnectField = bluetoothGatt.Class.GetDeclaredField("mAutoConnect");
//        autoConnectField.Accessible = true;
//        autoConnectField.SetBoolean(bluetoothGatt, true);
//    }
//    var connectMethod = bluetoothGatt.Class.GetDeclaredMethod(
//        "connect",
//        Java.Lang.Boolean.Type,
//        Class.FromType(typeof(BluetoothGattCallback))
//    );
//    connectMethod.Accessible = true;
//    var result = (bool)connectMethod.Invoke(bluetoothGatt, autoConnect, this.Callbacks);
//    return result;
//}


//BluetoothGatt CreateReflectionGatt(Java.Lang.Object bluetoothGatt)
//{
//    var ctor = Class
//        .FromType(typeof(BluetoothGatt))
//        .GetDeclaredConstructors()
//        .FirstOrDefault();

//    ctor.Accessible = true;
//    var parms = ctor.GetParameterTypes();
//    var args = new Java.Lang.Object[parms.Length];
//    for (var i = 0; i < parms.Length; i++)
//    {
//        var @class = parms[i].CanonicalName.ToLower();
//        switch (@class)
//        {
//            case "int":
//            case "integer":
//                args[i] = (int)BluetoothTransports.Le;
//                break;

//            case "android.bluetooth.ibluetoothgatt":
//                args[i] = bluetoothGatt;
//                break;

//            case "android.bluetooth.bluetoothdevice":
//                args[i] = this.NativeDevice;
//                break;

//            default:
//                args[i] = Android.App.Application.Context;
//                break;
//        }
//    }
//    var instance = (BluetoothGatt)ctor.NewInstance(args);
//    return instance;
//}


//BluetoothGatt ConnectGattCompat(bool autoConnect)
//{
//    var c = this.CentralContext.Android;
//    return c.IsMinApiLevel(23)
//        ? this.NativeDevice.ConnectGatt(c.AppContext, autoConnect, this.Callbacks, BluetoothTransports.Le)
//        : this.NativeDevice.ConnectGatt(c.AppContext, autoConnect, this.Callbacks);
//}