using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Shiny.BluetoothLE;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using GC = Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic;
#endif


namespace Shiny.Device.Tests.BluetoothLE
{
    public class PlatformTests
    {
        readonly ITestOutputHelper output;
        readonly IBleManager manager;


        public PlatformTests(ITestOutputHelper output)
        {
            this.output = output;
            //this.manager = new CentralManager();
        }


#if __ANDROID__

        [Theory]
        [MemberData(nameof(GetUuids))]
        public void UuidToGuidTests(string uuidString, byte[] bytes)
            => Assert.Equal(uuidString, bytes.ToGuid().ToString().ToUpper());


        public static IEnumerable<object[]> GetUuids()
        {
            yield return new object[]
            {
                "63331358-23C1-11E5-B696-FEFF819CDC9F",
                new byte[] { 0x9f, 0xdc, 0x9c, 0x81, 0xff, 0xfe, 0x96, 0xb6, 0xe5, 0x11, 0xc1, 0x23, 0x58, 0x13, 0x33, 0x63 }
            };
        }


        [Theory]
        [InlineData(BleFeatures.None, false)]
        [InlineData(BleFeatures.LowPoweredScan, true)]
        [InlineData(BleFeatures.OpenSettings, true)]
        [InlineData(BleFeatures.ViewPairedPeripherals, true)]
        [InlineData(BleFeatures.ControlAdapterState, true)]
        [InlineData(BleFeatures.PairingRequests, true)]
        [InlineData(BleFeatures.ReliableTransactions, true)]
        [InlineData(BleFeatures.MtuRequests, true)]
        public void FeatureCheck(BleFeatures feature, bool expectResult) =>
            Assert.Equal(this.manager.Features.HasFlag(feature), expectResult);


        [Fact]
        public void ControlAdapterStates()
        {
            var stateChanges = 0;
            this.manager.WhenStatusChanged().Subscribe(_ => stateChanges++);

            Assert.Equal(AccessState.Available, this.manager.Status);
            this.manager.SetAdapterState(false);

            Assert.Equal(AccessState.Disabled, this.manager.Status);
            this.manager.SetAdapterState(true);

            Assert.Equal(AccessState.Available, this.manager.Status);
            Assert.Equal(3, stateChanges);
        }

#endif


#if WINDOWS_UWP
[Fact]
        public async Task ConnectDisconnect()
        {
            var tcs = new TaskCompletionSource<ulong>();
            var adWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };
            var handler = new TypedEventHandler<BluetoothLEAdvertisementWatcher, BluetoothLEAdvertisementReceivedEventArgs>
                ((sender, args) =>
                {
                    if (args.Advertisement.LocalName.StartsWith("bean", StringComparison.InvariantCultureIgnoreCase))
                        tcs.TrySetResult(args.BluetoothAddress);
                }
            );
            adWatcher.Received += handler;
            adWatcher.Start();

            var bluetoothAddress = await tcs.Task;
            adWatcher.Received -= handler;
            adWatcher.Stop();

            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            this.output.WriteLine($"Bluetooth DeviceId: {device.BluetoothDeviceId.Id} - {device.DeviceId} / {device.Name}");

            var serviceResult = await device.GetGattServicesForUuidAsync(Guid.Parse("A495FF20-C5B1-4B44-B512-1370F02D74DE"));
            var service = serviceResult.Services.First();
            await service.OpenAsync(GattSharingMode.Exclusive);

            var characteristicResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            var characteristic = characteristicResult.Characteristics.First();

            var chtcs = new TaskCompletionSource<byte[]>();
            var handler2 = new TypedEventHandler<GC, GattValueChangedEventArgs>((sender, args) =>
                chtcs.TrySetResult(args.CharacteristicValue.ToArray())
            );
            characteristic.ValueChanged += handler2;
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            var data = await chtcs.Task;

            // start cleanup
            await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
            characteristic.ValueChanged -= handler2;

            //BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Connected)
            //service.Session.SessionStatusChanged += (sender, args) =>
            //{
            //    //args.Status == GattSessionStatus.Active
            //};
            //service.Session.MaintainConnection = true;
            //foreach (var c in characteristicResult.Characteristics)
            //{
            //    c.Service.Session.Dispose();
            //}
            //foreach (var s in serviceResult.Services)
            //{
            //    s.Session.Dispose();
            //}

            service.Dispose();
            service = null;
            characteristic = null;

            device.Dispose();
            device = null;

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }


        [Fact]
        public async Task CancelConnectionTest()
        {
            var pairedDevices = await this.manager.GetPairedPeripherals();
            var known = pairedDevices.FirstOrDefault();

            if (known != null)
            {
                // Make sure that is not connected already
                var connectedDevices = await this.manager.GetConnectedPeripherals();
                var device = connectedDevices.FirstOrDefault(d => d.Uuid == known.Uuid);
                Assert.Null(device);

                await known.ConnectWait().ToTask();

                connectedDevices = await this.manager.GetConnectedPeripherals();
                device = connectedDevices.FirstOrDefault(d => d.Uuid == known.Uuid);
                Assert.NotNull(device);

                known.CancelConnection();
                // and also we have to dispose of (not to hold a reference of) to the peripheral returned from GetConnectedPeripherals() else GC will not collect it and it will not disconnect on Dispose.
                // As long as there is a reference to a peripheral (peripheral instance AS A SINGLETON) it will not disconnect. Even if we don't explicitly open a connection the second peripheral (third,... reference to the peripheral)
                // can still read and write to the BT it doesn't have to explicitly connect if it is already connected. So any BT Peripheral instantiation like FromIdAsync, FromBluetoothAddressAsync and in general any FromXXXX
                // will return the same BT with or without a connection.
                device.CancelConnection(); // to actually only call NativeDevice.Dispose(). // We can make the IPeripheral : IDisposable to simplify?

                // Give it a couple of seconds to disconnect
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(10));
                connectedDevices = await this.manager.GetConnectedPeripherals();
                device = connectedDevices.FirstOrDefault(d => d.Uuid == known.Uuid);

                Assert.Null(device);
            }
        }
#endif
    }
}