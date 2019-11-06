﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Logging;
using Android.Bluetooth;
using Android.OS;
using Java.Lang;
using Exception = System.Exception;
using System.Collections.Generic;

namespace Shiny.BluetoothLE.Central.Internals
{
    public class DeviceContext
    {
        readonly GattCallbacks callbacks;
        readonly Subject<BleException> connErrorSubject;
        CancellationTokenSource cancelSrc;


        public DeviceContext(CentralContext context, BluetoothDevice device)
        {
            this.CentralContext = context;
            this.NativeDevice = device;
            this.callbacks = new GattCallbacks();
            this.Actions = new ConcurrentQueue<Func<Task>>();
            this.connErrorSubject = new Subject<BleException>();
        }


        // TODO: eliminate this
        public GattCallbacks Callbacks => this.callbacks;

        public CentralContext CentralContext { get; }
        public BluetoothGatt Gatt { get; private set; }
        public BluetoothDevice NativeDevice { get; }

        public ConnectionState Status => this
            .CentralContext
            .Manager
            .GetConnectionState(this.NativeDevice, ProfileType.Gatt)
            .ToStatus();


        public ConcurrentQueue<Func<Task>> Actions { get; }
        public IObservable<BleException> ConnectionFailed => this.connErrorSubject; // TODO: need the device

        //public IObservable<ConnectionState> WhenConnectionStatusChanged() => this.callbacks.ConnectionStateChanged.Select(x => x.NewState.ToStatus());
        //public IObservable<GattCharacteristicEventArgs> WhenWrite(BluetoothGattCharacteristic native) => this.callbacks.CharacteristicWrite.Where(x => x.Characteristic.Equals(native));
        //public IObservable<GattCharacteristicEventArgs> WhenRead(BluetoothGattCharacteristic native) => this.callbacks.CharacteristicRead.Where(x => x.Characteristic.Equals(native));

        public void Connect(ConnectionConfig config) => this.InvokeOnMainThread(() =>
        {
            try
            {
                this.CleanUpQueue();
                this.CreateGatt(config?.AutoConnect ?? true);
                if (this.Gatt == null)
                    throw new BleException("GATT connection could not be established");

                var priority = config?.AndroidConnectionPriority ?? ConnectionPriority.Normal;
                if (priority != ConnectionPriority.Normal)
                    this.Gatt.RequestConnectionPriority(this.ToNative(priority));
            }
            catch (Exception ex)
            {
                //Log.Warn("Connection", "Connection to peripheral failed - " + ex);
                this.connErrorSubject.OnNext(new BleException("Failed to connect to peripheral", ex));
            }
        });


        public IObservable<T> Invoke<T>(IObservable<T> observable)
        {
            if (!this.CentralContext.Configuration.AndroidUseInternalSyncQueue)
                return observable;

            return Observable.Create<T>(ob =>
            {
                var cancel = false;
                this.Actions.Enqueue(async () =>
                {
                    if (cancel)
                        return;

                    try
                    {
                        var result = await observable
                            .ToTask(this.cancelSrc.Token)
                            .ConfigureAwait(false);
                        ob.Respond(result);
                    }
                    catch (Exception ex)
                    {
                        ob.OnError(ex);
                    }
                });
                this.ProcessQueue(); // fire and forget

                return () => cancel = true;
            });
        }


        //public void RefreshServices()
        //{
        //    if (this.Gatt == null) //|| !this.CentralContext.Configuration.AndroidRefreshServices)
        //        return;

        //    // https://stackoverflow.com/questions/22596951/how-to-programmatically-force-bluetooth-low-energy-service-discovery-on-android
        //    try
        //    {
        //        Log.Write(BleLogCategory.Device, "Try to clear Android cache");
        //        var method = this.Gatt.Class.GetMethod("refresh");
        //        if (method != null)
        //        {
        //            var result = (bool)method.Invoke(this.Gatt);
        //            Log.Write(BleLogCategory.Device, "Cache result = " + result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Write(BleLogCategory.Device, "Failed to refresh services - " + ex);
        //    }
        //}


        public void InvokeOnMainThread(Action action)
        {
            if (this.CentralContext.Configuration.AndroidShouldInvokeOnMainThread)
                action.Dispatch();
            else
                action();
        }


        public void Close()
        {
            try
            {
                this.CleanUpQueue();
                this.Gatt?.Close();
                this.Gatt = null;
            }
            catch (Exception ex)
            {
                Log.Write(BleLogCategory.Device, "Unclean disconnect - " + ex);
            }
        }


        bool running;
        async void ProcessQueue()
        {
            if (this.running)
                return;

            try
            {
                this.running = true;
                Func<Task> outTask = null;
                while (this.Actions.TryDequeue(out outTask) && this.running)
                {
                    await outTask();
                }
            }
            catch (TaskCanceledException)
            {
            }
            this.running = false;
        }


        void CleanUpQueue()
        {
            this.running = false;
            this.cancelSrc?.Cancel();
            this.cancelSrc = new CancellationTokenSource();
            this.Actions.Clear();
        }


        void CreateGatt(bool autoConnect)
        {
            try
            {
                // somewhat a copy of android-rxbluetoothle
                if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
                {
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                var bmMethod = BluetoothAdapter.DefaultAdapter.Class.GetDeclaredMethod("getBluetoothManager");
                bmMethod.Accessible = true;
                var bluetoothManager = bmMethod.Invoke(BluetoothAdapter.DefaultAdapter);

                var method = bluetoothManager.Class.GetDeclaredMethod("getBluetoothGatt");
                method.Accessible = true;
                var iBluetoothGatt = method.Invoke(bluetoothManager);

                if (iBluetoothGatt == null)
                {
                    //Log.Debug("Peripheral", "Unable to find getBluetoothGatt object");
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                var bluetoothGatt = this.CreateReflectionGatt(iBluetoothGatt);
                if (bluetoothGatt == null)
                {
                    //Log.Info("Peripheral", "Unable to create GATT object via reflection");
                    this.Gatt = this.ConnectGattCompat(autoConnect);
                    return;
                }

                this.Gatt = bluetoothGatt;
                var connectSuccess = this.ConnectUsingReflection(this.Gatt, true);
                if (!connectSuccess)
                {
                    //Log.Error("Peripheral", "Unable to connect using reflection method");
                    this.Gatt?.Close();
                }
            }
            catch (Exception)
            {
                //Log.Info("Peripheral", "Defaulting to gatt connect compatible method - " + ex);
                this.Gatt = this.ConnectGattCompat(autoConnect);
            }
        }


        bool ConnectUsingReflection(BluetoothGatt bluetoothGatt, bool autoConnect)
        {
            if (autoConnect)
            {
                var autoConnectField = bluetoothGatt.Class.GetDeclaredField("mAutoConnect");
                autoConnectField.Accessible = true;
                autoConnectField.SetBoolean(bluetoothGatt, true);
            }
            var connectMethod = bluetoothGatt.Class.GetDeclaredMethod(
                "connect",
                Java.Lang.Boolean.Type,
                Class.FromType(typeof(BluetoothGattCallback))
            );
            connectMethod.Accessible = true;
            var result = (bool)connectMethod.Invoke(bluetoothGatt, autoConnect, this.Callbacks);
            return result;
        }


        BluetoothGatt CreateReflectionGatt(Java.Lang.Object bluetoothGatt)
        {
            var ctor = Class
                .FromType(typeof(BluetoothGatt))
                .GetDeclaredConstructors()
                .FirstOrDefault();

            ctor.Accessible = true;
            var parms = ctor.GetParameterTypes();
            var args = new Java.Lang.Object[parms.Length];
            for (var i = 0; i < parms.Length; i++)
            {
                var @class = parms[i].CanonicalName.ToLower();
                switch (@class)
                {
                    case "int":
                    case "integer":
                        args[i] = (int)BluetoothTransports.Le;
                        break;

                    case "android.bluetooth.ibluetoothgatt":
                        args[i] = bluetoothGatt;
                        break;

                    case "android.bluetooth.bluetoothdevice":
                        args[i] = this.NativeDevice;
                        break;

                    default:
                        args[i] = Android.App.Application.Context;
                        break;
                }
            }
            var instance = (BluetoothGatt)ctor.NewInstance(args);
            return instance;
        }


        BluetoothGatt ConnectGattCompat(bool autoConnect) => Build.VERSION.SdkInt >= BuildVersionCodes.M
            ? this.NativeDevice.ConnectGatt(Android.App.Application.Context, autoConnect, this.Callbacks, BluetoothTransports.Le)
            : this.NativeDevice.ConnectGatt(Android.App.Application.Context, autoConnect, this.Callbacks);


        GattConnectionPriority ToNative(ConnectionPriority priority)
        {
            switch (priority)
            {
                case ConnectionPriority.Low:
                    return GattConnectionPriority.LowPower;

                case ConnectionPriority.High:
                    return GattConnectionPriority.High;

                default:
                    return GattConnectionPriority.Balanced;
            }
        }
    }
}

