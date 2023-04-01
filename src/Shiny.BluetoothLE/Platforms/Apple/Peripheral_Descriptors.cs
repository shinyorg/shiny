using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => Observable.Create<IReadOnlyList<BleDescriptorInfo>>(ob =>
    {
        //            var handler = new EventHandler<CBCharacteristicEventArgs>((sender, args) =>
        //            {
        //                if (this.NativeCharacteristic.Descriptors == null)
        //                    return;

        //                var list = this.NativeCharacteristic
        //                    .Descriptors
        //                    .Select(native => new GattDescriptor(this, native))
        //                    .Distinct()
        //                    .Cast<IGattDescriptor>()
        //                    .ToList();

        //                ob.Respond(list);
        //            });
        //            this.Peripheral.DiscoveredDescriptor += handler;
        //            this.Peripheral.DiscoverDescriptors(this.NativeCharacteristic);

        //            return () => this.Peripheral.DiscoveredDescriptor -= handler;
        return () => { };
    });


    public IObservable<byte[]> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => Observable.Create<byte[]>(ob =>
    {
        var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
        {
            //if (!this.Equals(args.Descriptor))
            //    return;

            if (args.Error != null)
            {
                //ob.OnError(new BleException(args.Error.Description));
            }
            else
            {
                var value = args.Descriptor.ToByteArray();
                //ob.Respond<GattDescriptorResult>(new GattDescriptorResult(this, value));
            }
        });
        this.Native.UpdatedValue += handler;
        //this.Native.ReadValue(this.native);

        return () => this.Native.UpdatedValue -= handler;
    });


    public IObservable<Unit> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => Observable.Create<Unit>(ob =>
    {
        var handler = new EventHandler<CBDescriptorEventArgs>((sender, args) =>
        {
            //if (!this.Equals(args.Descriptor))
            //    return;

            if (args.Error != null)
            {
                ob.OnError(new BleException(args.Error.Description));
            }
            else
            {
                var bytes = args.Descriptor.ToByteArray();
                //ob.Respond(new GattDescriptorResult(this, bytes));
            }
        });

        var nsdata = NSData.FromArray(data);
        this.Native.WroteDescriptorValue += handler;
        //this.Native.WriteValue(nsdata, this.native);

        return () => this.Native.WroteDescriptorValue -= handler;
    });

    public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
    {
    }

    //bool Equals(CBDescriptor descriptor)
    //{
    //    if (!this.native.UUID.Equals(descriptor.UUID))
    //        return false;

    //    if (!this.NativeCharacteristic.UUID.Equals(descriptor.Characteristic.UUID))
    //        return false;

    //    if (!this.NativeService.UUID.Equals(descriptor.Characteristic.Service.UUID))
    //        return false;

    //    if (!this.Peripheral.Identifier.Equals(descriptor.Characteristic.Service.Peripheral.Identifier))
    //        return false;

    //    return true;
    //}


    //public override bool Equals(object obj)
    //{
    //    var other = obj as GattDescriptor;
    //    if (other == null)
    //        return false;

    //    if (!Object.ReferenceEquals(this, other))
    //        return false;

    //    return true;
    //}


    //public override int GetHashCode() => this.native.GetHashCode();
    //public override string ToString() => this.Uuid.ToString();
}