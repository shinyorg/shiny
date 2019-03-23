# Characteristics

Characteristics are the heart and soul of the BLE GATT connections.  You can read, write, and monitor the values (depending on permissions) with this plugin.

_It important thing with characteristics is NOT to store a reference to an object.  This reference becomes invalidated between connections to a device!_

## General Usage

**Discover characteristics on service**
```csharp
Service.DiscoverCharacteristics().Subscribe(characteristics => {});
```

**Read and Write to a characteristic**
```csharp
// once you have your characteristic instance from the service discovery
Characteristic.Read().Subscribe(
    result => {
        result.Data (byte[])
    },
    exception => {
        // reads can error!
    }
);

Characteristic.Write(bytes).Subscribe(
    result => {
        // you don't really need to do anything with the result
    },
    exception => {
        // writes can error!
    }
);
```

**Write Without Response**
```csharp
Characteristic.WriteWithoutResponse(bytes).Subscribe();
```


**Notifications**
```csharp
 // pass true to use indications (if available on android or UWP)
Characteristic.EnableNotifications(true);
var sub = characteristic
    .WhenNotificationReceived()
    .Subscribe(
        result => { result.Data... }
    );

// don't forget to turn them off when you're done
characteristic.DisableNotifications().Subscribe(); // pass true to enable indications if supported

// there are also some nice extensions to help speed this up
Characteristic.RegisterAndNotify().Subscribe(result => {
    // if you don't care when the notifications have been enabled
});

// From the device, this will attempt to discover a known service /w a known characteristic and hook that characteristic
// for notifications - 1 stop shop all the way
Device.HookCharacteristic(serviceUuid, characteristicUuid).Subscribe(result => {
});
```


**Discover descriptors on a characteristic**
```csharp
// once you have your characteristic instance from the service discovery.

var sub = characteristic.WhenDescriptorsDiscovered().Subscribe(bytes => {});

characteristic.WhenDescriptorsDiscovered().Subscribe(descriptor => {});
```

**BLOB Writes**

Used for sending larger arrays or streams of data without working with the MTU byte gap

```csharp
characteristic.BlobWrite(stream).Subscribe(x => 
{
	// subscription will give you current position and pulse every time a buffer is written
	// if write no response is used, a 100ms gap is placed after each write.  Note that this event will fire quicker as well
});


// same as above but with a byte array
characteristic.BlobWrite(bytes).Subscribe(x => {}); 
```

**Reliable Write Transactions**
```csharp
// TODO
```

## Extensions

```csharp

// register and subscribe to notifications
characteristic.RegisterAndNotify().Subscribe(result => {});

// read a characteristic on a given interval.  This is a substitute for SubscribeToNotifications()
characteristic.ReadInterval(TimeSpan).Subscribe(result => { result.Data... });

// discover all characteristics without finding services first
// you should not use this in production - it is more for easy discovery
// you should use GetKnownService/GetKnownCharacteristic
device.WhenAnyCharacteristicDiscovered().Subscribe(characteristic => {});

// will continue to read in a loop until ending bytes (argument) is detected
device.ReadUntil(new byte[] { 0x0 }).Subscribe(result => { result.Data... });
```
