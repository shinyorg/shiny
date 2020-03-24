Title: Connecting
Order: 2
---

Once you have scanned and found your peripheral, you can move on to connecting to it.  BLE operates on something called GATT (Generic Attributes).  

GATT is composed of 3 components
1. Services - this is a logical grouping of characteristics
2. Characteristics - this is the MAIN area of things that you will work with.  This is where the reading, writing, & notifications take place
3. Descriptors - these are less important in terms of use cases, but do support read/write scenarios.  They are grouped under each characteristic

## Peripheral.Connect() - void

Connect simply issues a request to connect.  It may connect now or days from now when the device is nearby.  You should use IBleCentralDelegate when using this method.  We'll talk more about that later

```csharp
peripheral.Connect();
```

If you need to wait for a connection and you know your device is nearby, you can use 

```csharp
await peripheral.ConnectAwait();
```

It is important to put your own timeout on these things using RX Timeout or supplying a cancellation token as shown below

```csharp
// this will throw an exception if it times out
await peripheral.Timeout(TimeSpan.FromSeconds(10)).ConnectAwait();

// or supply your own cancellation token
var cancelSource = new CancellationTokenSource();
await peripheral.ConnectAwait(cancelSource.Token);

cancelSource.Cancel();
```

