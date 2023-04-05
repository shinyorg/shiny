using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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


    readonly Subject<(CBDescriptor? Descriptor, NSError? Error)> descReadSubject = new();
    public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
        => this.descReadSubject.OnNext((descriptor, error));

    readonly Subject<(CBDescriptor? Descriptor, NSError? Error)> descWriteSubject = new();
    public override void WroteDescriptorValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError? error)
        => this.descWriteSubject.OnNext((descriptor, error));

    
    protected IObservable<CBDescriptor> GetNativeDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetNativeCharacteristic(serviceUuid, characteristicUuid)
        .Select(ch => Observable.Create<CBDescriptor>(ob =>
        {
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
            return () => { };
        }))
        .Switch();
}