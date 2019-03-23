# Services

Once connected to a device, you can initiate service discovery (it is pretty much all you can do against services).  It is important
not to hold a reference to a service in your code as it will be invalidated with new connections.  You can called for WhenServicesDiscovered() 
with or without a connection.  When the device becomes connected, it will initiate the discovery process.  Note that you can call WhenServicesDiscovered() repeatedly
and it will simply replay what has been discovered since the connection occurred.

**Discover services on a device**

```csharp
Device.WhenServicesDiscovered().Subscribe(service => 
{
});
```

**Skip the Services**

In a lot of cases, you won't care about the service discovery portion.  You can use the extension 
device.WhenAnyCharacteristicDiscovered() to bypass and just discover all characteristics across all services.
